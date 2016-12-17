namespace Networking.Core
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class TcpMessagePipe : IDisposable
    {
        private readonly Timer _timer;
        private readonly TcpClient _connection;
        private PacketStream _stream;
        private CancellationTokenSource _cancelReading;

        public TcpMessagePipe(TcpClient connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (!connection.Connected)
            {
                throw new ArgumentException("TcpClient should be already connected.", nameof(connection));
            }

            _connection = connection;
            _stream = new PacketStream(connection.GetStream());
            _timer = new Timer(state => ((TcpMessagePipe)state).CheckConnectionAsync(), this, (int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
        }

        public TcpMessagePipe()
        {
            _connection = new TcpClient();
            _timer = new Timer(state => ((TcpMessagePipe)state).CheckConnectionAsync(), this, Timeout.Infinite, Timeout.Infinite);
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
        }

        public event EventHandler<DefferedAsyncCompletedEventArgs> ConnectionFailure;

        public event EventHandler<DefferedAsyncResultEventArgs<IMessage>> MessageArrived;

        public bool IsReading { get; private set; }

        public TimeSpan KeepAliveTimeout { get; set; }

        public async Task StartReadingMessagesAsync()
        {
            if (IsReading)
            {
                throw new InvalidOperationException("Message pipe is already reading messages");
            }

            IsReading = true;
            _cancelReading = new CancellationTokenSource();
            while (!_cancelReading.IsCancellationRequested)
            {
                try
                {
                    var packet = await _stream.ReadPacketAsync(_cancelReading.Token).ConfigureAwait(false);
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

        public async Task ShutDownAsync()
        {
            if (!IsReading)
                throw new InvalidOperationException("Message pipe is not running");

            await DisconnectAsync(true).ConfigureAwait(false);
            _cancelReading.Cancel();
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            await _connection.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            _stream = new PacketStream(_connection.GetStream());
            _timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        public Task WriteMessageAsync(IMessage message)
        {
            return WriteMessageAsyncInternal(message);
        }

        public void Dispose()
        {
            _timer.Dispose();
            _connection.Close();
            _stream?.Dispose();
        }

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
            {
                return Task.FromResult(0);
            }

            handler(this, args);
            return args.WaitForDeferralsAsync();
        }

        private Task RaiseConnectionFailureAsync(DefferedAsyncCompletedEventArgs args)
        {
            var handler = ConnectionFailure;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            handler(this, args);
            return args.WaitForDeferralsAsync();
        }

        private async Task WriteMessageAsyncInternal(IMessage message)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            await _stream.WritePacketAsync(message.ToPacket(), CancellationToken.None).ConfigureAwait(false);
            _timer.Change((int)KeepAliveTimeout.TotalMilliseconds, Timeout.Infinite);
        }

        private Task DisconnectAsync(bool reuseSocket)
        {
            return Task.Factory.FromAsync(_connection.Client.BeginDisconnect, _connection.Client.EndDisconnect, reuseSocket, null);
        }
    }
}
