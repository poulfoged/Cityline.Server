using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Cityline.Server.Model;
using Cityline.Server;

namespace Cityline.Server.Writers
{

    internal class CitylineLineWriter : BaseCitylineWriter 
    {
        public CitylineLineWriter(SemaphoreSlim semaphore, ICitylineProducer provider, Stream stream, TicketHolder ticket, CancellationToken cancellationToken = default) : base(semaphore, provider, stream, ticket, cancellationToken) { }

        protected override async Task Write(TextWriter writer, string dataValue, ICitylineProducer provider, TicketHolder ticket, CancellationToken cancellationToken = default(CancellationToken))
        {
            await writer.WriteLineAsync($"id: {ticket.AsString()}");
            await writer.WriteLineAsync($"event: {provider.Name}");
            await writer.WriteLineAsync($"data: {dataValue}");
            await writer.WriteLineAsync("");
        }
    }
}