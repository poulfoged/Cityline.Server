using System;
using System.Threading.Tasks;

namespace Cityline.Server 
{
    public interface ICitylineWriter : IDisposable
    {
        Task Write(object value);
    }
}