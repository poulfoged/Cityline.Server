using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Cityline.Server.Model;
using System;
using Newtonsoft.Json.Converters;
using System.Net.WebSockets;

namespace Cityline.Server.Writers
{

    public class CitylineWriter : ICitylineWriter, IDisposable
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            Converters = new JsonConverter[] { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        private readonly SemaphoreSlim _semaphore;
        private readonly ICitylineProducer _provider;
        private readonly TicketHolder _ticket;
        private readonly CancellationToken _cancellationToken;
        private readonly WebSocket _webSocket;

        public CitylineWriter(SemaphoreSlim semaphore, ICitylineProducer provider, WebSocket socket, TicketHolder ticket, CancellationToken cancellationToken = default)
        {
            _semaphore = semaphore;
            _provider = provider;
            _webSocket = socket;
            _ticket = ticket;
            _cancellationToken = cancellationToken;
        }

        private async Task Write(WebSocket socket, string dataValue, ICitylineProducer provider, TicketHolder ticket, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);

            using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.None, AutoCompleteOnClose = true })
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

            var buffer = new ArraySegment<byte>(stream.ToArray(), 0, (int)stream.Length);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task Write(Object obj)
        {
            var value = JsonConvert.SerializeObject(obj, _settings);

            if (_cancellationToken.IsCancellationRequested)
                return;

            await _semaphore.WaitAsync(_cancellationToken);
            try
            {
                await Write(_webSocket, value, _provider, _ticket, _cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
        }
    }
}