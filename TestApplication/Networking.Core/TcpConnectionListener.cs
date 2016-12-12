using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Networking.Core
{
    public class TcpConnectionListener : IDisposable
    {
        TcpListener listener;

        public TcpConnectionListener(IPEndPoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            listener = new TcpListener(endpoint);
        }

        public void Start()
        {
            listener.Start();
        }

        public async Task<TcpConnection> AcceptTcpConnectionAsync()
        {
            var tcpClient = await listener.AcceptTcpClientAsync();
            return new TcpConnection(tcpClient);
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
