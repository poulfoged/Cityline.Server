using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cityline.Server.Model;
using Cityline.Server.Writers;

namespace Cityline.Server
{
    public interface ICitylineProducer
    {
        string Name { get; }
        int Priority { get; }
        Task Run(TicketHolder ticketHolder, IContext context, ICitylineWriter writer, CancellationToken cancellationToken);
    }
}