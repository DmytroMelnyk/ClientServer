using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Core
{
    public class TcpConnection : IDisposable
    {
        TcpClient tcpClient;
        PacketStream stream;

        public TcpConnection()
        {
            tcpClient = new TcpClient();
        }

        public TcpConnection(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);
            stream = new PacketStream(tcpClient.GetStream());
        }

        public Task WritePacketAsync(byte[] packet, CancellationToken ct)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            return stream.WritePacketAsync(packet, ct);
        }

        public Task<byte[]> ReadPacketAsync(CancellationToken ct)
        {
            return stream.ReadPacketAsync(ct);
        }

        public void Dispose()
        {
            stream?.Dispose();
            tcpClient.Close();
        }
    }
}
