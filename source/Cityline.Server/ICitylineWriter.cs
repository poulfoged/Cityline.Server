using System.Threading.Tasks;

namespace Cityline.Server
{
    public interface ICitylineWriter
    {
        Task Write(object obj);
    }
}