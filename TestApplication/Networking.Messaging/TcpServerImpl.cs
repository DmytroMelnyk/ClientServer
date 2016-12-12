using Networking.Core;
using Networking.Messaging.ConnectionProcessors;
using Networking.Messaging.Helpers;
using Networking.Messaging.MessageProcessors;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Messaging
{
    public class TcpServerImpl : IDisposable
    {
        private TcpConnectionListener listener;
        private ConcurrentSet<TcpMessagePipe> connections = new ConcurrentSet<TcpMessagePipe>();

        public TcpServerImpl(IPEndPoint endpoint)
        {
            listener = new TcpConnectionListener(endpoint);
        }

        public async void StartListening()
        {
            listener.Start();
            while (true)
            {
                try
                {
                    var tcpConnection = await listener.AcceptTcpConnectionAsync();
                    var newConnection = new TcpMessagePipe(tcpConnection);
                    newConnection.InvalidMessage += OnInvalidMessage;
                    newConnection.MessageArrived += OnMessageArrived;
                    newConnection.ReadFailure += OnReadFailure;
                    newConnection.WriteFailure += OnWriteFailure;
                    newConnection.StartReadingMessages();
                    connections.TryAdd(newConnection);
                }
                catch
                {

                }
            }
        }

        private void OnWriteFailure(object sender, IMessage e)
        {
            Dispose((TcpMessagePipe)sender);
        }

        private void OnReadFailure(object sender, EventArgs e)
        {
            Dispose((TcpMessagePipe)sender);
        }

        private void OnMessageArrived(object sender, IMessage e)
        {
            if (e.GetType() == typeof(KeepAliveMessage))
                return;

            Console.WriteLine("Message arrived");
            foreach (var connection in connections)
                Task.Factory.StartNew(state => connection.WriteMessageAsync((IMessage)state), e, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void OnInvalidMessage(object sender, EventArgs e)
        {
            Dispose((TcpMessagePipe)sender);
        }

        private void Dispose(TcpMessagePipe connection)
        {
            connections.TryRemove(connection);
            connection.InvalidMessage -= OnInvalidMessage;
            connection.MessageArrived -= OnMessageArrived;
            connection.ReadFailure -= OnReadFailure;
            connection.WriteFailure -= OnWriteFailure;
            connection.Dispose();
        }

        public void Dispose()
        {
            foreach (var connection in connections)
                connection.Dispose();

            listener.Dispose();
        }
    }
}
