using System;
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

        public event EventHandler<IMessage> WriteFailure;
        public event EventHandler<AsyncResultEventArgs<IMessage>> MessageArrived;

        public bool IsReading { get; private set; }

        public TcpMessagePipe(TcpClient connection)
        {
            this.connection = connection;
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
            timer = new Timer(_ => CheckConnection(), null, KeepAliveTimeout.Milliseconds, Timeout.Infinite);
        }

        public TcpMessagePipe() :
            this(new TcpClient())
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            try
            {
                if (endpoint == null)
                    throw new ArgumentNullException(nameof(endpoint));

                await connection.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
                stream = new PacketStream(connection.GetStream());
            }
            finally
            {
                timer.Change(KeepAliveTimeout.Milliseconds, Timeout.Infinite);
            }
        }

        public TimeSpan KeepAliveTimeout { get; set; }

        private Task CheckConnection()
        {
            return WriteMessageAsyncInternal(KeepAliveMessage.Instance);
        }

        public Task WriteMessageAsync(IMessage message)
        {
            return WriteMessageAsyncInternal(message);
        }

        private async Task WriteMessageAsyncInternal(IMessage message)
        {
            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                await stream.WritePacketAsync(message.ToPacket(), CancellationToken.None).ConfigureAwait(false);
                timer.Change(KeepAliveTimeout.Milliseconds, Timeout.Infinite);
            }
            catch
            {
                WriteFailure?.Invoke(this, message);
            }
        }

        public async void StartReadingMessagesAsync()
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
