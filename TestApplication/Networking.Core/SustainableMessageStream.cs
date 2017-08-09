namespace Networking.Core
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class SustainableMessageStream : IDisposable
    {
        private readonly SustainablePacketStream _stream;
        private readonly AsyncLock _writeLock;

        public SustainableMessageStream(SustainablePacketStream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _writeLock = new AsyncLock();
            Messages = _stream.Messages.Select(x => x.ToMessage());
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

        public void Dispose() => _stream.Dispose();

        private async Task WritePacketAsyncImpl(byte[] message, CancellationToken ct)
        {
            using (await _writeLock.LockAsync())
            {
                await _stream.WritePacketAsync(message, ct);
            }
        }
    }
}
