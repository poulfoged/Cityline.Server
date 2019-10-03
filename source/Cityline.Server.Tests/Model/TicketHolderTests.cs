using Cityline.Server.Model;
using Cityline.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cityline.Model.Tests
{
    [TestClass]
    public class TicketHolderTests
    {
        [TestMethod]
        public void Can_serialize_ticket()
        {
            ////Arrange
            var sample = new SampleClass { Name = "bob" };
            var ticketHolder = new TicketHolder();
            ticketHolder.UpdateTicket(sample);
            var ticketSource = ticketHolder.AsString();

            ////Act
            var result = new TicketHolder(ticketSource);

            ////Assert
            var actualSample = result.GetTicket<SampleClass>();
            Assert.AreEqual("bob", actualSample.Name);
        }
    }

    public class SampleClass 
    {
        public string Name { get; set; }
    }
}
