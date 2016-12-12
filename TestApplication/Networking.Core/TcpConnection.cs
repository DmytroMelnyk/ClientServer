using System;
using System.IO;
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

        private class PacketStream : IDisposable
        {
            public Stream Stream { get; }

            public PacketStream(Stream stream)
            {
                if (stream == null)
                    throw new ArgumentNullException(nameof(stream));

                this.Stream = stream;
            }

            public async Task WritePacketAsync(byte[] packet, CancellationToken ct)
            {
                if (packet == null)
                    throw new ArgumentNullException(nameof(packet));

                byte[] lengthPrefix = BitConverter.GetBytes(packet.Length);
                await Stream.WriteAsync(lengthPrefix, 0, sizeof(int), ct).ConfigureAwait(false);

                if (packet.Length != 0)
                    await Stream.WriteAsync(packet, 0, packet.Length, ct).ConfigureAwait(false);
            }

            private async Task<byte[]> ReadStreamAsync(int bytesCount, CancellationToken ct)
            {
                var buffer = new byte[bytesCount];
                for (int totalBytesReceived = 0; totalBytesReceived < bytesCount;)
                {
                    int bytesReceived = await this.Stream.ReadAsync(buffer, totalBytesReceived, bytesCount - totalBytesReceived, ct).ConfigureAwait(false);
                    if (bytesReceived == 0)
                        return null;

                    totalBytesReceived += bytesReceived;
                }

                return buffer;
            }

            public async Task<byte[]> ReadPacketAsync(CancellationToken ct)
            {
                // Read packet header
                byte[] lengthBuffer = await ReadStreamAsync(sizeof(int), ct);

                if (lengthBuffer == null)
                    return null;

                int length = BitConverter.ToInt32(lengthBuffer, 0);

                if (length < 0)
                    throw new InvalidDataException("Packet length less than zero (corrupted message)");

                // Zero-length packets are allowed as keepalives
                if (length == 0)
                    return new byte[0];

                // read packet body
                return await ReadStreamAsync(length, ct);
            }

            public void Dispose()
            {
                Stream.Dispose();
            }
        }
    }
}
