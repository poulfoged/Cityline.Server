using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Cityline.Server
{
	public class PingConsumer : ICitylineConsumer<string>
    {
        public string Name => "_ping";

        public int Priority => 10;

        public Task Receive(string payload, IContext context, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Ping recieved " + payload);
            return Task.CompletedTask;
        }
    }
}

