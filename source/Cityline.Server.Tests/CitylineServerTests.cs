using Cityline.Server.Model;
using Cityline.Server;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Net.Sockets;
using Cityline.Server.Writers;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using System.Text;

namespace Cityline.Tests
{
    public class CitylineServerTests
    {
        private readonly ITestOutputHelper _output;

        static CitylineServerTests()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        public CitylineServerTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact]
        public async Task Can_clean_up() 
        {
            //// Arrange
            WeakReference reference = new WeakReference(new CitylineServer(new[] { new SampleProvider() }));
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1);

            var socket = WebSocket.CreateFromStream(MemoryStream.Null, true, null, TimeSpan.Zero);
            await (reference.Target as CitylineServer).WriteStream(socket, new CitylineRequest(), null, cancellationTokenSource.Token);

            /// Act
            (reference.Target as IDisposable).Dispose();
            socket = null;
            await Task.Delay(1);
            GC.Collect();

            //// Assert
            reference.IsAlive.Should().Be(false);
        }

        [Fact]
        public async Task Can_write_to_stream()
        {
            ////Arrange
            var service = new CitylineServer(new[] { new SampleProvider() });
            var stream = new MemoryStream();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            var socket = WebSocket.CreateFromStream(stream, true, null, TimeSpan.Zero);

            ////Act
            await service.WriteStream(socket, new CitylineRequest(), null, cancellationTokenSource.Token);

            ////Assert
            stream.Position = 2; // not that WebSocket's is adding 2 bytes at the beginning for some reason
            string json;
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
                json = await reader.ReadLineAsync();

            var payload = JsonConvert.DeserializeObject<ParseHelper>(json);
            payload.Event.Should().Be("sample");

        }



        [Fact]
        public async Task Can_write_large_json_to_stream()
        {
            ////Arrange
            var sampleObject = new
            {
                Test = "dimmer",
                Numbers = Enumerable.Range(0, 1000).ToDictionary(m => $"row-{m}", m => $"value-{m}")
            };

            var service = new CitylineServer(new[] { new SampleProvider { sampleObject = sampleObject } });
            var stream = new MemoryStream();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000); 
            var socket = WebSocket.CreateFromStream(stream, true, null, TimeSpan.Zero);

            ////Act
            await service.WriteStream(socket, new CitylineRequest(), null, cancellationTokenSource.Token);

            ////Assert
            stream.Position = 4; // what is that, a size indicator?

            string json;
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            {
                json = await reader.ReadLineAsync();
            }

            var parsed = JsonConvert.DeserializeObject<ParseHelper>(json);
            var parsedData = JsonConvert.DeserializeObject<ParseHelper2>(parsed.Data);

            parsedData.Numbers.Values.Should().Contain("value-999");
        }
    }

    class ParseHelper 
    {
        public string Event { get; set; }
        public string Id { get; set; }
        public string Data { get; set; }
    }

    class ParseHelper2 
    {
       public Dictionary<string, string> Numbers {get;set;}
    }

    public class SampleProvider : ICitylineProducer
    {
        public string Name => "sample";

        public int Priority => 0;

        public object sampleObject = new { hello = "world"};

        public async Task Run(TicketHolder ticketHolder, IContext context, CitylineWriter writer, CancellationToken cancellationToken)
        {
            var myState = ticketHolder.GetTicket<MyState>();

            ticketHolder.UpdateTicket(new { created = DateTime.UtcNow });

            await writer.Write(sampleObject);
        }

        class MyState {}
    }
}