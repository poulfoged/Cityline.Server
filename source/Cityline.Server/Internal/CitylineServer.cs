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

        internal static int _instanceCount;

        public CitylineServer(IEnumerable<ICitylineProducer> providers, TextWriter logger = null)
        {
            _providers = providers.OrderBy(m => m.Priority);
            _logger = logger ?? TextWriter.Null;
            Interlocked.Increment(ref _instanceCount);
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
            var tasks = new ConcurrentDictionary<string, Task>();

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

                    tasks.TryAdd(name, Task.Run(async () =>
                    {
                        try
                        {
                            await RunProducer(provider, socket, ticket, context, cancellationToken);

                            if (request.Tickets.ContainsKey(name))
                                request.Tickets[name] = ticket.AsString();
                            else
                                request.Tickets.Add(name, ticket.AsString());
                        }
                        catch (Exception ex)
                        {
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
                    .ContinueWith(task => {
                        var result = tasks.TryRemove(name, out Task value);

                        if (result)
                            task?.Dispose();

                    }, cancellationToken));
                }
                await Task.Delay(10, cancellationToken);
            }
            await Task.Run(() => Task.WaitAll(tasks.Values.ToArray()), cancellationToken);
        }

        private async Task RunProducer(ICitylineProducer producer, WebSocket socket, TicketHolder ticket, IContext context, CancellationToken cancellationToken)
        {
            using CitylineWriter writer = new(semaphore, producer, socket, ticket, cancellationToken);
            await producer.Run(ticket, context, writer, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);


        }

        private bool alreadyDisposed = false;

        public void Dispose(bool explicitCall)
        {
            if (!this.alreadyDisposed)
            {
                if (explicitCall)
                {
                    System.Console.WriteLine("Not in the destructor, " +
                     "so cleaning up other objects.");
                    // Not in the destructor, so we can reference other objects.

                    semaphore?.Dispose();
                    _logger.Dispose();
                }
                // Perform standard cleanup here...
                System.Console.WriteLine("Cleaning up.");
            }
            alreadyDisposed = true;
        }

        ~CitylineServer()
        {
            System.Console.WriteLine("In the destructor now.");
            Dispose(false);
            Interlocked.Decrement(ref _instanceCount);
        }
    }
}
