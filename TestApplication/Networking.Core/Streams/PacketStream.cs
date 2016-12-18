namespace Networking.Core.Streams
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncPrimitives;
    using Utils;

    public class PacketStream : IDisposable
    {
        private readonly AsyncLock _readLock = new AsyncLock();
        private readonly AsyncLock _writeLock = new AsyncLock();

        public PacketStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Stream = stream;
        }

        public Stream Stream { get; }

        public async Task WritePacketAsync(byte[] packet, CancellationToken ct)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var lengthPrefix = BitConverter.GetBytes(packet.Length);

            using (await _readLock.LockAsync().ConfigureAwait(false))
            {
                await Stream.WriteAsync(lengthPrefix, 0, sizeof(int), ct).ConfigureAwait(false);

                if (packet.Length != 0)
                {
                    await Stream.WriteAsync(packet, 0, packet.Length, ct).ConfigureAwait(false);
                }
            }
        }

        public async Task<byte[]> ReadPacketAsync(CancellationToken ct)
        {
            using (await _writeLock.LockAsync().ConfigureAwait(false))
            {
                // Read packet header
                var lengthBuffer = await ReadStreamAsync(sizeof(int), ct).ConfigureAwait(false);

                if (lengthBuffer == null)
                {
                    return null;
                }

                var length = BitConverter.ToInt32(lengthBuffer, 0);

                if (length < 0)
                {
                    throw new InvalidDataException("Packet length less than zero (corrupted message)");
                }

                // Zero-length packets are allowed as keepalives
                if (length == 0)
                {
                    return Util.ZeroLengthPacket;
                }

                // read packet body
                return await ReadStreamAsync(length, ct).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        private async Task<byte[]> ReadStreamAsync(int bytesCount, CancellationToken ct)
        {
            var buffer = new byte[bytesCount];
            for (var totalBytesReceived = 0; totalBytesReceived < bytesCount;)
            {
                var bytesReceived = await Stream.ReadAsync(buffer, totalBytesReceived, bytesCount - totalBytesReceived, ct).ConfigureAwait(false);
                if (bytesReceived == 0)
                {
                    return null;
                }

                totalBytesReceived += bytesReceived;
            }

            return buffer;
        }
    }
}
