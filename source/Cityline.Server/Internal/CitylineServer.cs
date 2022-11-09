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
using System.Net.WebSockets;

namespace Cityline.Server
{
     public class CitylineServer : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly IEnumerable<ICitylineProducer> _providers;
        private readonly TextWriter _logger;
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None };

        public CitylineServer(IEnumerable<ICitylineProducer> providers, TextWriter logger = null)
        {
            _providers = providers.OrderBy(m => m.Priority);
            _logger = logger ?? TextWriter.Null;
        }

        public async Task WriteStream(WebSocket socket, CitylineRequest request, IContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await DoWriteStream(socket, request, context, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }

        public async Task DoWriteStream(WebSocket socket, CitylineRequest request, IContext context, CancellationToken cancellationToken = default)
        {
            ConcurrentDictionary<Task, object> tasks = new ConcurrentDictionary<Task, object>();

            var queue = new ConcurrentQueue<ICitylineProducer>(_providers);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!queue.IsEmpty)
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
                            await RunProducer(provider, socket, ticket, context, cancellationToken);

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
                    }, cancellationToken).ContinueWith(task =>
                    {
                        _logger.WriteLine($"Task failed for {provider.Name} failed: {task.Exception}");

                        if (task.Exception != null)
                            throw task.Exception;
                    }, cancellationToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current)
                    .ContinueWith(task => tasks.TryRemove(task, out object value)), null);
#pragma warning restore 4014
                }

                await Task.Delay(10, cancellationToken);
            }

            // await Task.WhenAll(tasks.Keys);

            await Task.Run(()=> Task.WaitAll(tasks.Keys.ToArray()), cancellationToken);
        }

        private async Task RunProducer(ICitylineProducer producer, WebSocket socket, TicketHolder ticket, IContext context, CancellationToken cancellationToken)
        {
            using CitylineWriter writer = new(semaphore, producer, socket, ticket, cancellationToken);
            await producer.Run(ticket, context, writer, cancellationToken);        
        }

        public void Dispose()
        {
            semaphore?.Dispose();
            _logger.Dispose();
        }
    }
}
