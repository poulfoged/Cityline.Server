using Cityline.Server.Model;
using Cityline.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Numerics;
using System.Text;

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

        static ushort GetShardId(string key)
        {
            using(var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
                var integer = BigInteger.Abs(new BigInteger(hash));
                return (ushort)(integer % ushort.MaxValue);
            }
        }
    }

    public class SampleClass 
    {
        public string Name { get; set; }
    }
}
