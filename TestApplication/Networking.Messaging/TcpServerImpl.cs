using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Networking.Core;

namespace Networking.Server
{
    public class TcpServerImpl : IDisposable
    {
        private TcpListener listener;
        private ConcurrentSet<TcpMessagePipe> connections = new ConcurrentSet<TcpMessagePipe>();

        public TcpServerImpl(IPEndPoint endpoint)
        {
            listener = new TcpListener(endpoint);
        }

        public void StartListening()
        {
            listener.Start();
            while (true)
            {
                try
                {
                    var tcpClient = listener.AcceptTcpClient();
                    var connection = new TcpMessagePipe(tcpClient);
                    ConnectionAccepted(connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void ConnectionAccepted(TcpMessagePipe newConnection)
        {
            newConnection.MessageArrived += OnMessageArrived;
            newConnection.WriteFailure += OnWriteFailure;
            newConnection.StartReadingMessagesAsync();
            connections.TryAdd(newConnection);
            Console.WriteLine("New connection was added");
        }

        private void OnWriteFailure(object sender, IMessage e)
        {
            Dispose((TcpMessagePipe)sender);
        }

        private void OnMessageArrived(object sender, AsyncResultEventArgs<IMessage> e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Reading was cancelled");
                return;
            }

            if (e.Error != null)
            {
                Dispose((TcpMessagePipe)sender);
                return;
            }

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
            connection.MessageArrived -= OnMessageArrived;
            connection.WriteFailure -= OnWriteFailure;
            connection.Dispose();
        }

        public void Dispose()
        {
            foreach (var connection in connections)
                connection.Dispose();

            listener.Stop();
        }
    }
}
