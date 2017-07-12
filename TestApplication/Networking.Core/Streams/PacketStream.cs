namespace Networking.Core.Streams
{
    using System;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncPrimitives;
    using Utils;

    public class PacketStream : IDisposable
    {
        public PacketStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Stream = stream;
        }

        public Stream Stream { get; }

        public Task WritePacketAsync(byte[] packet, CancellationToken ct)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return WritePacketAsyncImpl(packet, ct);
        }

        public Task<byte[]> ReadPacketAsync(IProgress<double> progress, CancellationToken ct)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return ReadPacketAsyncImpl(progress, ct);
        }

        private async Task<byte[]> ReadPacketAsync(CancellationToken ct)
        {
            // Read packet header
            var lengthBuffer = await ReadStreamAsync(sizeof(int), ct).ConfigureAwait(false);

            var length = BitConverter.ToInt32(lengthBuffer, 0);

            if (length < 0)
            {
                throw new InvalidDataException("Packet length less than zero (corrupted stream state)");
            }

            // Zero-length packets are allowed as keepalives
            if (length == 0)
            {
                return Util.ZeroLengthPacket;
            }

            // read packet body
            return await ReadStreamAsync(length, ct).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        private async Task<byte[]> ReadPacketAsyncImpl(IProgress<double> progress, CancellationToken ct)
        {
            // Read packet header
            var lengthBuffer = await ReadStreamAsync(sizeof(int), ct).ConfigureAwait(false);
            progress.Report(0.0);

            var length = BitConverter.ToInt32(lengthBuffer, 0);

            if (length < 0)
            {
                throw new InvalidDataException("Packet length less than zero (corrupted stream state)");
            }

            // Zero-length packets are allowed as keepalives
            if (length == 0)
            {
                progress.Report(1.0);
                return Util.ZeroLengthPacket;
            }

            // read packet body
            return await ReadStreamAsync(length, progress, ct).ConfigureAwait(false);
        }

        private async Task WritePacketAsyncImpl(byte[] packet, CancellationToken ct)
        {
            var lengthPrefix = BitConverter.GetBytes(packet.Length);

            await Stream.WriteAsync(lengthPrefix, 0, sizeof(int), ct).ConfigureAwait(false);

            if (packet.Length != 0)
            {
                await Stream.WriteAsync(packet, 0, packet.Length, ct).ConfigureAwait(false);
            }
        }

        private async Task<byte[]> ReadStreamAsync(int bytesCount, CancellationToken ct)
        {
            var buffer = new byte[bytesCount];
            for (var totalBytesReceived = 0; totalBytesReceived < bytesCount;
                totalBytesReceived += await Stream.ReadAsync(buffer, totalBytesReceived, bytesCount - totalBytesReceived, ct))
            {
            }

            return buffer;
        }

        private async Task<byte[]> ReadStreamAsync(int bytesCount, IProgress<double> progress, CancellationToken ct)
        {
            var buffer = new byte[bytesCount];
            for (var totalBytesReceived = 0; totalBytesReceived < bytesCount;
                totalBytesReceived += await Stream.ReadAsync(buffer, totalBytesReceived, bytesCount - totalBytesReceived, ct))
            {
                progress.Report((double)totalBytesReceived / bytesCount);
            }

            return buffer;
        }
    }
}
