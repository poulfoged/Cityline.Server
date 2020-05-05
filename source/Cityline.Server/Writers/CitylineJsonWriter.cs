using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Cityline.Server.Model;

namespace Cityline.Server.Writers
{
    internal class CitylineJsonWriter : BaseCitylineWriter 
    {
        public CitylineJsonWriter(SemaphoreSlim semaphore, ICitylineProducer provider, Stream stream, TicketHolder ticket, CancellationToken cancellationToken = default) : base(semaphore, provider, stream, ticket, cancellationToken) { }

        protected override async Task Write(TextWriter writer, string dataValue, ICitylineProducer provider, TicketHolder ticket, CancellationToken cancellationToken = default(CancellationToken))
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
    }
}