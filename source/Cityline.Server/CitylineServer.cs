using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Cityline.Server.Model;
using System.Collections.Concurrent;
using System.Linq;
using System;
using Cityline.Server.Writers;

namespace Cityline.Server
{
    public class CitylineServer
    {
        public bool UseJson { get; set; }
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly IEnumerable<ICitylineProducer> _providers;
        private readonly TextWriter _logger;
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None };

        public CitylineServer(IEnumerable<ICitylineProducer> providers, TextWriter logger = null)
        {
            _providers = providers.OrderBy(m => m.Priority);
            _logger = logger ?? TextWriter.Null;
        }

        public async Task WriteStream(Stream stream, CitylineRequest request, IContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await DoWriteStream(stream, request, context, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }

        public async Task DoWriteStream(Stream stream, CitylineRequest request, IContext context, CancellationToken cancellationToken = default)
        {
            ConcurrentDictionary<Task, object> tasks = new ConcurrentDictionary<Task, object>();

            var queue = new ConcurrentQueue<ICitylineProducer>(_providers);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (queue.Count > 0)
                {
                    if (!queue.TryDequeue(out ICitylineProducer provider))
                        continue;

                    var name = provider.Name;
                    TicketHolder ticket = null;

                    if (request.Tickets == null)
                        request.Tickets = new Dictionary<string, string>();

                    if (request.Tickets.ContainsKey(name))
                        ticket = new TicketHolder(request.Tickets[name]);

                    ticket = ticket ?? new TicketHolder();

#pragma warning disable 4014
                    tasks.TryAdd(Task.Run(async () =>
                    {
                        try
                        {
                            var ownSource = new CancellationTokenSource();
                            cancellationToken.Register(ownSource.Cancel);

                            await RunProducer(provider, stream, ticket, context, ownSource.Token);

                            if (request.Tickets.ContainsKey(name))
                                request.Tickets[name] = ticket.AsString();
                            else
                                request.Tickets.Add(name, ticket.AsString());
                        }
                        catch(Exception ex) {
                            _logger.WriteLine($"Producer {provider.Name} failed: {ex}");
                        }
                        finally
                        {
                            queue.Enqueue(provider);
                        }
                    }).ContinueWith(task =>
                    {
                        _logger.WriteLine($"Task failed for {provider.Name} failed: {task.Exception}");

                        if (task.Exception != null)
                            throw task.Exception;
                    }, cancellationToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current)
                    .ContinueWith(task => tasks.TryRemove(task, out object value)), null);
#pragma warning restore 4014
                }

                await Task.Delay(200, cancellationToken);
            }

            await Task.WhenAll(tasks.Keys);
        }



        private async Task WriteJson(TextWriter writer, string dataValue, ICitylineProducer provider, TicketHolder ticket, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.None })
            {
                await jsonWriter.WriteStartObjectAsync(cancellationToken);
                await jsonWriter.WritePropertyNameAsync("id", cancellationToken);
                await jsonWriter.WriteValueAsync(ticket.AsString(), cancellationToken);
                await jsonWriter.WritePropertyNameAsync("event", cancellationToken);
                await jsonWriter.WriteValueAsync(provider.Name, cancellationToken);
                await jsonWriter.WritePropertyNameAsync("data", cancellationToken);
                await jsonWriter.WriteValueAsync(dataValue, cancellationToken);
                await jsonWriter.WriteEndObjectAsync(cancellationToken);
                await writer.WriteAsync('\n');
            }
        }

        private async Task WriteText(TextWriter writer, string dataValue, ICitylineProducer provider, TicketHolder ticket, CancellationToken cancellationToken = default(CancellationToken))
        {
            await writer.WriteLineAsync($"id: {ticket.AsString()}");
            await writer.WriteLineAsync($"event: {provider.Name}");
            await writer.WriteLineAsync($"data: {dataValue}");
            await writer.WriteLineAsync("");
        }

        private async Task RunProducer(ICitylineProducer provider, Stream stream, TicketHolder ticket, IContext context, CancellationToken cancellationToken = default)
        {
            ICitylineWriter writer;
            
            if (this.UseJson)
                writer = new CitylineJsonWriter(semaphore, provider, stream, ticket, cancellationToken);
            else
                writer = new CitylineLineWriter(semaphore, provider, stream, ticket, cancellationToken);
 
            var response = await provider.GetFrame(ticket, context, cancellationToken);

            if (response == null)
                return;

            await writer.Write(response);
        }
    }
}
