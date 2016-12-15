using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Core
{
    public class PacketStream : IDisposable
    {
        public Stream Stream { get; }
        private readonly AsyncLock m_readLock = new AsyncLock();
        private readonly AsyncLock m_writeLock = new AsyncLock();

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

            using (await m_readLock.LockAsync().ConfigureAwait(false))
            {
                await Stream.WriteAsync(lengthPrefix, 0, sizeof(int), ct).ConfigureAwait(false);

                if (packet.Length != 0)
                    await Stream.WriteAsync(packet, 0, packet.Length, ct).ConfigureAwait(false);
            }
        }

        private async Task<byte[]> ReadStreamAsync(int bytesCount, CancellationToken ct)
        {
            var buffer = new byte[bytesCount];
            for (int totalBytesReceived = 0; totalBytesReceived < bytesCount;)
            {
                int bytesReceived = await Stream.ReadAsync(buffer, totalBytesReceived, bytesCount - totalBytesReceived, ct).ConfigureAwait(false);
                if (bytesReceived == 0)
                    return null;

                totalBytesReceived += bytesReceived;
            }

            return buffer;
        }

        public async Task<byte[]> ReadPacketAsync(CancellationToken ct)
        {
            using (await m_writeLock.LockAsync().ConfigureAwait(false))
            {
                // Read packet header
                byte[] lengthBuffer = await ReadStreamAsync(sizeof(int), ct).ConfigureAwait(false);

                if (lengthBuffer == null)
                    return null;

                int length = BitConverter.ToInt32(lengthBuffer, 0);

                if (length < 0)
                    throw new InvalidDataException("Packet length less than zero (corrupted message)");

                // Zero-length packets are allowed as keepalives
                if (length == 0)
                    return new byte[0];

                // read packet body
                return await ReadStreamAsync(length, ct).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
