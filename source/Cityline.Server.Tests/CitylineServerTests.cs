using Cityline.Server.Model;
using Cityline.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;

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
    }

    public class SampleProvider : ICitylineProducer
    {
        public string Name => "sample";

        public Task<object> GetFrame(ITicketHolder ticket, IContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var myState = ticket.GetTicket<MyState>();

            ticket.UpdateTicket(new { created = DateTime.UtcNow });

            return Task.FromResult((object)new { hello = "world"});
        }

        class MyState {}
    }
}
