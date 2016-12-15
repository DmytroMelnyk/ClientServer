using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Core
{
    public sealed class TcpMessagePipe : IDisposable
    {
        private Timer timer;
        private TcpClient connection;
        private PacketStream stream;
        private CancellationTokenSource cancelReading;
        private TaskCompletionSource<object> readingCancellation;

        public event EventHandler<DefferedAsyncCompletedEventArgs> ConnectionFailure;
        public event EventHandler<DefferedAsyncResultEventArgs<IMessage>> MessageArrived;

        public bool IsReading { get; private set; }

        public TcpMessagePipe(TcpClient connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (!connection.Connected)
                throw new ArgumentException("TcpClient should be already connected.", nameof(connection));

            this.connection = connection;
            stream = new PacketStream(connection.GetStream());
            timer = new Timer(_ => CheckConnectionAsync(), null, (int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
        }

        public TcpMessagePipe()
        {
            this.connection = new TcpClient();
            timer = new Timer(_ => CheckConnectionAsync(), null, Timeout.Infinite, Timeout.Infinite);
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            await connection.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            stream = new PacketStream(connection.GetStream());
            timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        public TimeSpan KeepAliveTimeout { get; set; }

        private async void CheckConnectionAsync()
        {
            try
            {
                await WriteMessageAsyncInternal(KeepAliveMessage.Instance).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await RaiseConnectionFailureAsync(new DefferedAsyncCompletedEventArgs(ex, false, null)).ConfigureAwait(false);
            }
        }

        private Task RaiseMessageArrivedAsync(DefferedAsyncResultEventArgs<IMessage> args)
        {
            var handler = MessageArrived;
            if (handler == null)
                return Task.FromResult(0);

            handler(this, args);
            return args.WaitForDeferralsAsync();
        }

        private Task RaiseConnectionFailureAsync(DefferedAsyncCompletedEventArgs args)
        {
            var handler = ConnectionFailure;
            if (handler == null)
                return Task.FromResult(0);

            handler(this, args);
            return args.WaitForDeferralsAsync();
        }

        public Task WriteMessageAsync(IMessage message)
        {
            return WriteMessageAsyncInternal(message);
        }

        private async Task WriteMessageAsyncInternal(IMessage message)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            await stream.WritePacketAsync(message.ToPacket(), CancellationToken.None).ConfigureAwait(false);
            timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        public async Task StartReadingMessagesAsync()
        {
            if (IsReading)
                throw new InvalidOperationException("Message pipe is already reading messages");

            IsReading = true;
            cancelReading = new CancellationTokenSource();
            while (!cancelReading.IsCancellationRequested)
            {
                try
                {
                    var packet = await stream.ReadPacketAsync(cancelReading.Token).ConfigureAwait(false);
                    var message = packet.ToMessage();
                    await RaiseMessageArrivedAsync(new DefferedAsyncResultEventArgs<IMessage>(message)).ConfigureAwait(false);
                }
                catch (InvalidCastException)
                {
                    await RaiseMessageArrivedAsync(new DefferedAsyncResultEventArgs<IMessage>(new InvalidDataException("Unknown type of message received"))).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await RaiseMessageArrivedAsync(new DefferedAsyncResultEventArgs<IMessage>(ex)).ConfigureAwait(false);
                }
            }
            IsReading = false;
        }

        public void StopReading()
        {
            if (!IsReading)
                throw new InvalidOperationException("Message pipe is not running");

            cancelReading.Cancel();
        }

        public void Dispose()
        {
            timer.Dispose();
            connection.Close();
            stream?.Dispose();
        }
    }
}
