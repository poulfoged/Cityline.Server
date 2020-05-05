using System.Threading;
using System.Threading.Tasks;
using Cityline.Server.Writers;

namespace Cityline.Server
{
    public interface ICitylineProducer
    {
        string Name { get; }
        Task<object> GetFrame(ITicketHolder ticketHolder, IContext context, ICitylineWriter writer, CancellationToken cancellationToken = default(CancellationToken)); 
    }
}