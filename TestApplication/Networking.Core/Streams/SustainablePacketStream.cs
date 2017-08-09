namespace Networking.Core.Streams
{
    using System;
    using System.IO;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Utils;

    public class SustainablePacketStream : IDisposable
    {
        private readonly Subject<bool> _reading = new Subject<bool>();
        private readonly Subject<bool> _writing = new Subject<bool>();
        private readonly Stream _stream;

        public SustainablePacketStream(Stream stream, TimeSpan keepAlivePeriod)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            KeepAliveTimeout = keepAlivePeriod;

            Messages = Observable
                .FromAsync(ReadPacketAsync, Scheduler.CurrentThread)
                .Repeat()
                .Merge(KeepAlives())
                .Where(x => x.Length != 0);
        }

        private IObservable<byte[]> KeepAlives() => _writing
            .ToPulsar(TimeSpan.FromSeconds(1))
            .Merge(_reading.ToPulsar(TimeSpan.FromSeconds(1)))
            .Window(KeepAliveTimeout)
            .SelectMany(x => x.Any())
            .Where(x => !x)
            .SelectMany(_ => WriteKeepAliveAsync())
            .Materialize()
            .Do(Console.WriteLine)
            .Dematerialize();

        public IObservable<byte[]> Messages { get; }

        public TimeSpan KeepAliveTimeout { get; }

        public Task WritePacketAsync(byte[] packet, CancellationToken ct)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return WritePacketAsyncImpl(packet, ct);
        }

        public void Dispose() => _stream.Dispose();

        private Task<byte[]> ReadPacketAsync(CancellationToken ct) => ReadPacketAsyncImpl(ct);

        private async Task<byte[]> ReadPacketAsyncImpl(CancellationToken ct)
        {
            // Read packet header
            var lengthBuffer = await ReadStreamAsync(sizeof(int), ct).ConfigureAwait(false);

            using (new OperationNotifier(_reading))
            {
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
        }

        private async Task WritePacketAsyncImpl(byte[] packet, CancellationToken ct)
        {
            using (new OperationNotifier(_writing))
            {
                var lengthPrefix = BitConverter.GetBytes(packet.Length);
                await _stream.WriteAsync(lengthPrefix, 0, sizeof(int), ct).ConfigureAwait(false);
                if (packet.Length != 0)
                {
                    await _stream.WriteAsync(packet, 0, packet.Length, ct).ConfigureAwait(false);
                }
            }
        }

        private Task<byte[]> WriteKeepAliveAsync() =>
            WritePacketAsync(Util.ZeroLengthPacket, CancellationToken.None)
                .ContinueWith(_ => Util.ZeroLengthPacket, TaskContinuationOptions.ExecuteSynchronously);

        private async Task<byte[]> ReadStreamAsync(int bytesCount, CancellationToken ct)
        {
            var buffer = new byte[bytesCount];
            for (var totalBytesReceived = 0; totalBytesReceived < bytesCount;
                totalBytesReceived += await _stream.ReadAsync(buffer, totalBytesReceived, bytesCount - totalBytesReceived, ct))
            {
            }

            return buffer;
        }

        private struct OperationNotifier : IDisposable
        {
            private readonly IObserver<bool> _observer;

            public OperationNotifier(IObserver<bool> observer)
            {
                _observer = observer ?? throw new ArgumentNullException(nameof(observer));
                _observer.OnNext(true);
            }

            public void Dispose() => _observer.OnNext(false);
        }
    }
}
