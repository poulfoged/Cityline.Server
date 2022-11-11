using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Cityline.Server.Model;

namespace Cityline.Server
{
    public class Context : IContext
    {
        public IPrincipal User { get; set; }
        public Uri RequestUrl { get; set; }
        public Func<object, Task> Emit { get; set; }
        public Dictionary<string, string> Headers { get; internal set; }
        public CitylineRequest Request { get; internal set; }
    }
}