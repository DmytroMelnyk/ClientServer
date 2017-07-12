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

    public class SustainablePacketStream: IDisposable
    {
        private readonly Subject<bool> reading;
        private readonly Subject<bool> writing;
        private readonly AsyncLock writeLock;

        public SustainablePacketStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Stream = stream;
            writeLock = new AsyncLock();
            writing = new Subject<bool>();
            reading = new Subject<bool>();
            Messages = Observable.Create<Unit>(observer =>
            {
                Timer timer = null;
                timer = new Timer(
                    _ =>
                    {
                        observer.OnNext(Unit.Default);
                        timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
                    }, null, (int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);

                var disposable = reading.CombineLatest(writing, (reading, writing) => reading || writing)
                    .DistinctUntilChanged()
                    .Subscribe(isBusy =>
                    {
                        if (isBusy)
                        {
                            timer.Change(Timeout.Infinite, Timeout.Infinite);
                        }
                        else
                        {
                            timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
                        }
                    });

                writing.OnNext(false);
                reading.OnNext(false);

                return Disposable.Create(() =>
                {
                    disposable.Dispose();
                    timer.Dispose();
                });
            })
            .SelectMany(_ => WritePacketAsync(Util.ZeroLengthPacket, CancellationToken.None).ToObservable())
            .Select(_ => Util.ZeroLengthPacket)
            .Merge(Observable.FromAsync(ReadPacketAsync).Repeat())
            .Where(packet => packet.Length != 0);
        }

        public Stream Stream { get; }

        public IObservable<byte[]> Messages { get; }

        public TimeSpan KeepAliveTimeout { get; set; }

        public Task WritePacketAsync(byte[] packet, CancellationToken ct)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return WritePacketAsyncImpl(packet.ToPacket(), ct);
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        private async Task<byte[]> ReadPacketAsync(CancellationToken ct)
        {
            // Read packet header
            var lengthBuffer = await ReadStreamAsync(sizeof(int), ct).ConfigureAwait(false);

            reading.OnNext(true);

            var length = BitConverter.ToInt32(lengthBuffer, 0);

            if (length < 0)
            {
                throw new InvalidDataException("Packet length less than zero (corrupted stream state)");
            }

            // Zero-length packets are allowed as keepalives
            if (length == 0)
            {
                reading.OnNext(false);
                return Util.ZeroLengthPacket;
            }

            // read packet body
            var body = await ReadStreamAsync(length, ct).ConfigureAwait(false);
            reading.OnNext(false);
            return body;
        }

        private async Task WritePacketAsyncImpl(byte[] packet, CancellationToken ct)
        {
            using (await writeLock.LockAsync())
            {
                writing.OnNext(true);
                var lengthPrefix = BitConverter.GetBytes(packet.Length);

                await Stream.WriteAsync(lengthPrefix, 0, sizeof(int), ct).ConfigureAwait(false);

                if (packet.Length != 0)
                {
                    await Stream.WriteAsync(packet, 0, packet.Length, ct).ConfigureAwait(false);
                }

                writing.OnNext(false);
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
    }
}
