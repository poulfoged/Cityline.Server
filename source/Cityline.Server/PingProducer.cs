using System;
using System.Threading;
using System.Threading.Tasks;
using Cityline.Server.Model;
using Cityline.Server.Writers;

namespace Cityline.Server
{
	public class PingProducer : ICitylineProducer
    {
        public int Priority { get; } = 100;

        public string Name => "ping";

        public async Task Run(TicketHolder ticketHolder, IContext context, ICitylineWriter writer, CancellationToken cancellationToken)
        {
            var ticket = ticketHolder.GetTicket<PingTicket>();

            if (ticket != null && (DateTime.UtcNow - ticket.Created).TotalSeconds < 5)
                return;

            ticketHolder.UpdateTicket(new PingTicket { Created = DateTime.UtcNow });

            var result = new { PingCreated = DateTime.UtcNow };

            await writer.Write(result);
        }

        public class PingTicket
        {
            public DateTime Created { get; set; }
        }
    }
}

