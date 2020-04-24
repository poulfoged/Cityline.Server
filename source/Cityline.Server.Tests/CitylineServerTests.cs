using Cityline.Server.Model;
using Cityline.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;

namespace Cityline.Tests
{
    [TestClass]
    public class CitylineServerTests
    {
        static CitylineServerTests()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        [TestMethod]
        public async Task Can_write_to_stream()
        {
            ////Arrange
            var service = new CitylineServer(new[] { new SampleProvider()});
            var stream = new MemoryStream();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            
            ////Act
            await service.WriteStream(stream, new CitylineRequest(), null, cancellationTokenSource.Token);

            ////Assert
            stream.Position = 0;
            string eventName;
            string eventData;
            string eventTicket;

            using (var reader = new StreamReader(stream)) {
                eventTicket = await reader.ReadLineAsync();
                eventName = await reader.ReadLineAsync();
                eventData = await reader.ReadLineAsync();
            }

            Assert.AreEqual("event: sample", eventName);
        }

        [TestMethod]
        public async Task Can_write_json_to_stream()
        {
            ////Arrange
            var service = new CitylineServer(new[] { new SampleProvider()}) { UseJson = true};
            var stream = new MemoryStream();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            
            ////Act
            await service.WriteStream(stream, new CitylineRequest(), null, cancellationTokenSource.Token);

            ////Assert
            stream.Position = 0;
            string json;
            using (var reader = new StreamReader(stream)) 
            {
                json = await reader.ReadLineAsync();
            }

            var parsed = JsonConvert.DeserializeObject<ParseHelper>(json);

            Assert.AreEqual("sample", parsed.Event);
        }

        [TestMethod]
        public async Task Can_write_large_json_to_stream()
        {
            ////Arrange

            var sampleObject = new 
            {
                Test = "dimmer",
                Numbers = Enumerable.Range(0, 1000).ToDictionary(m => $"row-{m}", m => $"value-{m}")
            };

            var service = new CitylineServer(new[] { new SampleProvider { sampleObject = sampleObject }}) { UseJson = true};
            var stream = new MemoryStream();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            
            ////Act
            await service.WriteStream(stream, new CitylineRequest(), null, cancellationTokenSource.Token);

            ////Assert
            stream.Position = 0;
            string json;
            using (var reader = new StreamReader(stream)) 
            {
                json = await reader.ReadLineAsync();
            }

            var parsed = JsonConvert.DeserializeObject<ParseHelper>(json);
            var parsedData = JsonConvert.DeserializeObject<ParseHelper2>(parsed.Data);

            Assert.AreEqual("value-999", parsedData.Numbers["row-999"]);
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

        public object sampleObject = new { hello = "world"};

        public Task<object> GetFrame(ITicketHolder ticket, IContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var myState = ticket.GetTicket<MyState>();

            ticket.UpdateTicket(new { created = DateTime.UtcNow });

            return Task.FromResult(sampleObject);
        }

        class MyState {}
    }
}
