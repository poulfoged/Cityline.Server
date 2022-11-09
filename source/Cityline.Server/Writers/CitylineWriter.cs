using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Cityline.Server.Model;
using System;
using Newtonsoft.Json.Converters;

namespace Cityline.Server.Writers
{
    internal abstract class BaseCitylineWriter : ICitylineWriter
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings 
        { 
            ContractResolver = new CamelCasePropertyNamesContractResolver(), 
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        private readonly SemaphoreSlim semaphore;
        private readonly ICitylineProducer provider;
        private readonly Stream stream;
        private readonly TicketHolder ticket;
        private readonly CancellationToken cancellationToken;

        protected BaseCitylineWriter(SemaphoreSlim semaphore, ICitylineProducer provider, Stream stream, TicketHolder ticket, CancellationToken cancellationToken = default)
        {
            this.semaphore = semaphore;
            this.provider = provider;
            this.stream = stream;
            this.stream = stream;
            this.ticket = ticket;
            this.cancellationToken = cancellationToken;
        }

        protected abstract Task Write(TextWriter writer, string dataValue, ICitylineProducer provider, TicketHolder ticket, CancellationToken cancellationToken);

        public async Task Write(Object obj)
        {
            var value = JsonConvert.SerializeObject(obj, settings);
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), value.Length + 1024, true))
                    await Write(writer, value, provider, ticket, CancellationToken.None); // CancellationToken.None, we don't want partial messages
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}