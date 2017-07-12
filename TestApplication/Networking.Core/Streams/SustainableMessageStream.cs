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

    public sealed class SustainableMessageStream : IDisposable
    {
        private readonly PacketStream _stream;
        private readonly Subject<bool> writing;
        private readonly TimerObservable timer;
        private readonly AsyncLock writeLock;

        public SustainableMessageStream(PacketStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            writeLock = new AsyncLock();
            writing = new Subject<bool>();
            var reading = new Subject<double>();

            timer = new TimerObservable(writing, reading);
            Messages = timer
                .Timer
                .SelectMany(_ => WritePacketAsyncImpl(Util.ZeroLengthPacket, CancellationToken.None).ToObservable())
                .Select(_ => Util.ZeroLengthPacket)
                .Merge(Observable.FromAsync(() => _stream.ReadPacketAsync(reading.ToProgress(), CancellationToken.None)).Repeat())
                .Where(packet => packet.Length != 0)
                .Select(Util.ToMessage);
        }

        public SustainableMessageStream(Stream stream)
            : this(new PacketStream(stream))
        {
        }

        public TimeSpan KeepAliveTimeout
        {
            get { return timer.KeepAliveTimeout; }
            set { timer.KeepAliveTimeout = value; }
        }

        public IObservable<object> Messages { get; }

        public Task WriteMessageAsync(object message, CancellationToken ct)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return WritePacketAsyncImpl(message.ToPacket(), ct);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        private async Task WritePacketAsyncImpl(byte[] message, CancellationToken ct)
        {
            using (await writeLock.LockAsync())
            {
                writing.OnNext(true);
                await _stream.WritePacketAsync(message, ct);
                writing.OnNext(false);
            }
        }

        public static IObservable<TResult> MyCombineLatest<TLeft, TRight, TResult>
(
IObservable<TLeft> left,
IObservable<TRight> right,
Func<TLeft, TRight, TResult> resultSelector
)
        {
            Observable
                .Interval(TimeSpan.FromSeconds(0.1))
                .Select(_ => Console.KeyAvailable)
                .Where(x => x)
                .Throttle(TimeSpan.FromSeconds(5));

            Observable
                .Interval(TimeSpan.FromMilliseconds(100))
                .Throttle(TimeSpan.FromSeconds(1));

            var refcountedLeft = left.Publish().RefCount();
            var refcountedRight = right.Publish().RefCount();
            return Observable.Join(
            refcountedLeft,
            refcountedRight,
            value => refcountedLeft,
            value => refcountedRight,
            resultSelector);
        }

        private class TimerObservable
        {
            public TimerObservable(IObservable<bool> writing, IObservable<double> reader)
            {
                reader
                        .Where(progressValue => progressValue == 0.0 || progressValue == 1.0)
                        .Select(x => x == 1.0)
                        .StartWith(false)
                        .CombineLatest(writing.StartWith(false), (isReading, isWriting) => isReading || isWriting)
                        .DistinctUntilChanged()
                        .CombineLatest(Observable.Interval(TimeSpan.FromSeconds(5)), (isBusy, _) => !isBusy);

                KeepAliveTimeout = TimeSpan.FromSeconds(5);

                Timer = Observable.Create<Unit>(observer =>
                {
                    Timer timer = null;
                    timer = new Timer(
                        _ =>
                        {
                            observer.OnNext(Unit.Default);
                            timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
                        }, null, (int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);

                    var disposable = reader
                        .Where(progressValue => progressValue == 0.0 || progressValue == 1.0)
                        .Select(x => x == 1.0)
                        .StartWith(false)
                        .CombineLatest(writing.StartWith(false), (isReading, isWriting) => isReading || isWriting)
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

                    return Disposable.Create(() =>
                    {
                        disposable.Dispose();
                        timer.Dispose();
                    });
                });
            }

            public TimeSpan KeepAliveTimeout { get; set; }

            public IObservable<Unit> Timer { get; }
        }
    }
}
