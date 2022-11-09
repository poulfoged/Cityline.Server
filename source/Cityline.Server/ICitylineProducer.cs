using System.Threading;
using System.Threading.Tasks;

namespace Cityline.Server
{
    public interface ICitylineProducer
    {
        string Name { get; }
        int Priority { get; }
        Task<object> GetFrame(ITicketHolder ticketHolder, IContext context, CancellationToken cancellationToken = default); 
    }
}