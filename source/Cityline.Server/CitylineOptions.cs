using System;

namespace Cityline.Server
{
    public class CitylineOptions
    {
        public string Path { get; set; } = "/cityline";
        
        public Action<Context> Authorization { get;  set; }
    }
}