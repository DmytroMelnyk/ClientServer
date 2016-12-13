using System.Net;
using System.Threading.Tasks;

namespace Networking.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            new TcpServerImpl(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50001)).StartListening();
        }
    }
}
