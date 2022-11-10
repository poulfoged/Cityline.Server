using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cityline.Server;
using Cityline.Server.Model;
using Newtonsoft.Json;

namespace Cityline.Server
{
    public class CitylineReciever : IDisposable
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _firstFrames = new();
        private readonly ConcurrentDictionary<string, Frame> _frames = new();
        private readonly IEnumerable<ICitylineConsumer> _consumers;
        private ClaimsPrincipal _principal;

        public CitylineReciever(IEnumerable<ICitylineConsumer> consumers)
        {
            _consumers = consumers.OrderBy(m => m.Priority);
        }

        public async Task<T> WaitFirstFrame<T>(string name, CancellationToken cancellationToken) 
        {
            _firstFrames.TryAdd(name, new SemaphoreSlim(0));
            await _firstFrames[name].WaitAsync(cancellationToken);
            
            try 
            {
                return JsonConvert.DeserializeObject<T>(_frames[name].Data);
            } 
            catch (Exception)
            {
                return default;
            }
        }

        public void StartBackground(WebSocket webSocket, CancellationToken cancellationToken) 
        {
            var _ = Task.Run(async () => await Listen(webSocket, cancellationToken), cancellationToken); 
        }

        public async Task Listen(WebSocket webSocket, CancellationToken cancellationToken)
        {
            IContext context = new Context();

            var serializer = new JsonSerializer();
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = null;

            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.Count == 0)
                    return;

                try
                {
                    using var reader = new StringReader(Encoding.UTF8.GetString(buffer));
                    using var jsonReader = new JsonTextReader(reader);
                    var frame = serializer.Deserialize<Frame>(jsonReader);

                     _frames.TryAdd(frame.Event, frame);
                    _firstFrames.TryAdd(frame.Event, new SemaphoreSlim(0));
                    _firstFrames[frame.Event].Release();

                    context = new Context { User = _principal };
                    if (context.User != null) // TODO: make this prittier
                    {
                        foreach (var consumer in _consumers) 
                        {
                            if (frame.Event == consumer.Name) 
                                await RunConsumer(consumer, frame, context, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    // log
                }

            } while (!cancellationToken.IsCancellationRequested);
        }

        internal void SetUser(ClaimsPrincipal principal)
        {
            _principal = principal;
        }

        internal static async Task RunConsumer(ICitylineConsumer consumer, Frame frame, IContext context, CancellationToken cancellationToken)
        {
            consumer.ThrowIfNull(nameof(consumer));
            frame.ThrowIfNull(nameof(frame));
            
            // find genric type via reflection
            var genericType = typeof(ICitylineConsumer<>);
            var genericInterface = Array.Find(consumer.GetType().GetInterfaces(), m => m.GetGenericTypeDefinition() == genericType);

            if (genericInterface == null)
                throw new ArgumentException($"CitylineConsumer must implement {genericType}.");

            Type itemType = genericInterface.GetGenericArguments()[0];
            object payload;
            try 
            { 
                payload = JsonConvert.DeserializeObject(frame.Data, itemType);
            } 
            catch (JsonReaderException ex) 
            {
                throw new Exception($"Unable to parse {nameof(Frame.Data)} to type {itemType} data is '{frame.Data}'.", ex);
            }

            var method = genericInterface.GetMethods().Single(m => m.Name == nameof(ICitylineConsumer<object>.Receive));
            try 
            { 
                await (Task)method.Invoke(consumer, new object[] { payload, context, cancellationToken } ); 
            } 
            catch (Exception ex) 
            {
                throw new Exception($"Consumer {consumer.Name} failed processing frame {frame}", ex);
            }
        }

        public void Dispose()
        {
            foreach (var firstFrame in this._firstFrames)
            {
                firstFrame.Value?.Dispose();
            }
        }
    }
}