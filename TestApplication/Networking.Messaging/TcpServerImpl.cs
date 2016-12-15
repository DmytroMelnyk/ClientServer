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
            newConnection.ConnectionFailure += OnWriteFailure;
            newConnection.StartReadingMessagesAsync();
            connections.TryAdd(newConnection);
            Console.WriteLine("New connection was added");
        }

        private void OnWriteFailure(object sender, DefferedAsyncCompletedEventArgs e)
        {
            var connection = (TcpMessagePipe)sender;
            connection.StopReading();
            Console.WriteLine("Client was disconnected");
            Dispose(connection);
        }

        private void OnMessageArrived(object sender, DefferedAsyncResultEventArgs<IMessage> e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Reading was cancelled");
                return;
            }

            if (e.Error != null)
            {
                var connection = (TcpMessagePipe)sender;
                connection.StopReading();
                Console.WriteLine("Client was disconnected");
                Dispose(connection);
                return;
            }

            Console.WriteLine($"{DateTime.Now}: Message arrived");
            //if (e.Result is KeepAliveMessage)
            //    return;

            foreach (var connection in connections)
            {
                Task.Factory.StartNew(
                    state => connection.WriteMessageAsync((IMessage)state),
                    e.Result,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default).Unwrap();
            }
        }

        private void Dispose(TcpMessagePipe connection)
        {
            connections.TryRemove(connection);
            connection.MessageArrived -= OnMessageArrived;
            connection.ConnectionFailure -= OnWriteFailure;
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
