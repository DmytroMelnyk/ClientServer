﻿namespace Networking.Core.Streams
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncEvents;
    using Utils;

    public sealed class SustainableMessageStream : IDisposable
    {
        private SustainablePacketStream _stream;

        public SustainableMessageStream(SustainablePacketStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            _stream.PacketArrived += OnPacketArrived;

            _stream.OnConnectionBreak += async (sender, e) =>
            {
                using (e.GetDeferral())
                {
                    await ConnectionFailure.RaiseAsync(this, e).ConfigureAwait(false);
                }
            };
        }

        public SustainableMessageStream(Stream stream)
            : this(new SustainablePacketStream(stream))
        {
        }

        public event EventHandler<DeferredAsyncCompletedEventArgs> ConnectionFailure;

        public event EventHandler<DeferredAsyncResultEventArgs<object>> MessageArrived;

        public void StartReadingMessages()
        {
            _stream.StartReadingLoopAsync();
        }

        public Task WriteMessageAsync(object message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _stream.WritePacketAsync(message.ToPacket(), CancellationToken.None);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        private async void OnPacketArrived(object sender, DeferredAsyncResultEventArgs<byte[]> e)
        {
            using (e.GetDeferral())
            {
                if (e.Error != null)
                {
                    await MessageArrived.RaiseAsync(this, new DeferredAsyncResultEventArgs<object>(e.Error)).ConfigureAwait(false);
                    return;
                }
                else if (e.Cancelled)
                {
                    await MessageArrived.RaiseAsync(this, new DeferredAsyncResultEventArgs<object>(e.Cancelled)).ConfigureAwait(false);
                    return;
                }

                object message = e.Result.ToMessage();
                await MessageArrived.RaiseAsync(this, new DeferredAsyncResultEventArgs<object>(message)).ConfigureAwait(false);
            }
        }
    }
}
