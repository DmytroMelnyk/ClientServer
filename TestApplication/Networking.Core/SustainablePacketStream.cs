namespace Networking.Core
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncEvents;

    public class SustainablePacketStream
    {
        private readonly Timer _timer;

        public SustainablePacketStream(PacketStream packetStream)
        {
            if (packetStream == null)
            {
                throw new ArgumentNullException(nameof(packetStream));
            }

            PacketStream = packetStream;
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
            _timer = new Timer(state => ((SustainablePacketStream)state).CheckConnectionAsync(), this, (int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        public SustainablePacketStream(Stream stream)
            : this(new PacketStream(stream))
        {
        }

        public event EventHandler<DeferredAsyncCompletedEventArgs> OnConnectionBreak;

        public event EventHandler<DeferredAsyncResultEventArgs<byte[]>> PacketArrived;

        public TimeSpan KeepAliveTimeout { get; set; }

        public PacketStream PacketStream { get; }

        public Task WritePacketAsync(byte[] packet, CancellationToken ct)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return WriteMessageInternalAsync(packet, ct);
        }

        public async void StartReadingLoopAsync()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        // if any exception happens at this point we should unwrap it. If read was successfull we should reset timer,
                        // if read was followed by InvalidDataException we should reset timer too.
                        var packet = await PacketStream.ReadPacketAsync(CancellationToken.None).ConfigureAwait(false);
                        _timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
                        await PacketArrived.RaiseAsync(this, new DeferredAsyncResultEventArgs<byte[]>(packet)).ConfigureAwait(false);
                    }
                    catch (InvalidDataException ex)
                    {
                        _timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
                        await PacketArrived.RaiseAsync(this, new DeferredAsyncResultEventArgs<byte[]>(ex)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                await PacketArrived.RaiseAsync(this, new DeferredAsyncResultEventArgs<byte[]>(ex)).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
            PacketStream.Dispose();
        }

        private async Task WriteMessageInternalAsync(byte[] packet, CancellationToken ct)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            // if any exception happens at this point we should unwrap it. It'll also prevent timer from turning on.
            await PacketStream.WritePacketAsync(packet, CancellationToken.None).ConfigureAwait(false);
            _timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        private async void CheckConnectionAsync()
        {
            try
            {
                await WriteMessageInternalAsync(Util.ZeroLengthPacket, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await OnConnectionBreak.RaiseAsync(this, new DeferredAsyncCompletedEventArgs(ex, false, null)).ConfigureAwait(false);
            }
        }
    }
}
