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

        public event EventHandler<AsyncCompletedEventArgs> ConnectionFailure;
        public event EventHandler<AsyncResultEventArgs<IMessage>> MessageArrived;

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
                ConnectionFailure?.Invoke(this, new AsyncCompletedEventArgs(ex, false, null));
            }
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
            readingCancellation = new TaskCompletionSource<object>();
            while (!cancelReading.IsCancellationRequested)
            {
                try
                {
                    var packet = await stream.ReadPacketAsync(cancelReading.Token).ConfigureAwait(false);
                    var message = packet.ToMessage();
                    MessageArrived?.Invoke(this, new AsyncResultEventArgs<IMessage>(message));
                }
                catch (InvalidCastException)
                {
                    MessageArrived?.Invoke(this, new AsyncResultEventArgs<IMessage>(new InvalidDataException("Unknown type of message received")));
                }
                catch (Exception ex)
                {
                    MessageArrived?.Invoke(this, new AsyncResultEventArgs<IMessage>(ex));
                }
            }
            IsReading = false;
            readingCancellation.SetResult(null);
        }

        public async Task StopReadingAsync()
        {
            if (!IsReading)
                throw new InvalidOperationException("Message pipe is not running");

            cancelReading.Cancel();
            await readingCancellation.Task;
        }

        public void Dispose()
        {
            timer.Dispose();
            connection.Close();
            stream?.Dispose();
        }
    }
}
