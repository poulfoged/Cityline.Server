using System.Threading;
using System.Threading.Tasks;
using Cityline.Server;

namespace Cityline.Server
{
    public interface ICitylineConsumer
    {
        string Name { get; }
        int Priority { get; }
    }

    public interface ICitylineConsumer<T> : ICitylineConsumer
    {
        Task Receive(T payload, IContext context, CancellationToken cancellationToken = default);
    }
}