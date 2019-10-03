using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Cityline.Server
{
    public interface IContext
    {
        IPrincipal User { get; set; }
        Uri RequestUrl { get; set; }
    }
}