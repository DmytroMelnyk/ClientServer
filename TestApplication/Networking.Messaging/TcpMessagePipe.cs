using Networking.Core;
using Networking.Messaging.ConnectionProcessors;
using Networking.Messaging.Helpers;
using Networking.Messaging.MessageProcessors;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Messaging
{
    public sealed class TcpMessagePipe : IDisposable
    {
        private Timer timer;
        private TcpConnection connection;
        public event EventHandler<IMessage> WriteFailure;
        public event EventHandler<EventArgs> ReadFailure;
        public event EventHandler<EventArgs> InvalidMessage;
        public event EventHandler<IMessage> MessageArrived;

        public TcpMessagePipe(TcpConnection connection)
        {
            this.connection = connection;
            KeepAliveTimeout = TimeSpan.FromSeconds(5);
            timer = new Timer(_ => CheckConnection(), null, KeepAliveTimeout.Milliseconds, Timeout.Infinite);
        }

        public TcpMessagePipe() :
            this(new TcpConnection())
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public Task ConnectAsync(IPEndPoint endpoint)
        {
            try
            {
                if (endpoint == null)
                    throw new ArgumentNullException(nameof(endpoint));

                return connection.ConnectAsync(endpoint);
            }
            finally
            {
                timer.Change(KeepAliveTimeout.Milliseconds, Timeout.Infinite);
            }
        }

        public TimeSpan KeepAliveTimeout { get; set; }

        private Task CheckConnection()
        {
            return WriteMessageAsyncInternal(new KeepAliveMessage(), CancellationToken.None);
        }

        public Task WriteMessageAsync(IMessage message)
        {
            return WriteMessageAsyncInternal(message, CancellationToken.None);
        }

        private async Task WriteMessageAsyncInternal(IMessage message, CancellationToken ct)
        {
            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                await connection.WritePacketAsync(message.ToPacket(), ct);
                timer.Change(KeepAliveTimeout.Milliseconds, Timeout.Infinite);
            }
            catch (ObjectDisposedException ex)
            {
                // Do nothing. Object was disposed
            }
            catch (OperationCanceledException ex)
            {
                timer.Change(KeepAliveTimeout.Milliseconds, Timeout.Infinite);
                // Do something else if needed
            }
            catch
            {
                WriteFailure?.Invoke(this, message);
            }
        }

        public async void StartReadingMessages()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var packet = await connection.ReadPacketAsync(CancellationToken.None);
                        MessageArrived?.Invoke(this, packet.ToMessage());
                    }
                    catch (OperationCanceledException ex)
                    {
                        // In case if stop reading functionality is needed
                    }
                }
            }
            catch (InvalidCastException ex)
            {
                InvalidMessage?.Invoke(this, EventArgs.Empty);
            }
            catch (ObjectDisposedException ex)
            {
                // Do nothing. Object was disposed
            }
            catch
            {
                ReadFailure?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            timer.Dispose();
            connection.Dispose();
        }
    }
}
