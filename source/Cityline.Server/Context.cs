using System;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Cityline.Server
{
    public class Context : IContext
    {
        public IPrincipal User { get; set; }
        public Uri RequestUrl { get; set; }
        public Func<object, Task> Emit { get; set; }
    }
}