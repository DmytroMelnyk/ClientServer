namespace Networking.Server
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Core.AsyncEvents;
    using Core.Messages;

    public class TcpServerImpl : IDisposable
    {
        private TcpListener listener;
        private ConcurrentSet<SustainableMessageStream> connections = new ConcurrentSet<SustainableMessageStream>();

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
                    var connection = new SustainableMessageStream(tcpClient.GetStream());
                    ConnectionAccepted(connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void Dispose()
        {
            foreach (var connection in connections)
            {
                connection.Dispose();
            }

            listener.Stop();
        }

        private void ConnectionAccepted(SustainableMessageStream newConnection)
        {
            newConnection.MessageArrived += OnMessageArrived;
            newConnection.ConnectionFailure += OnWriteFailure;
            newConnection.StartReadingMessages();
            connections.TryAdd(newConnection);
            Console.WriteLine("New connection was added");
        }

        private void OnWriteFailure(object sender, DeferredAsyncCompletedEventArgs e)
        {
            using (e.GetDeferral())
            {
                var connection = (SustainableMessageStream)sender;
                connection.Dispose();
                Console.WriteLine("Client was disconnected");
                Dispose(connection);
            }
        }

        private void OnMessageArrived(object sender, DeferredAsyncResultEventArgs<IMessage> e)
        {
            using (e.GetDeferral())
            {
                if (e.Cancelled)
                {
                    Console.WriteLine("Reading was cancelled");
                    return;
                }

                if (e.Error != null)
                {
                    var connection = (SustainableMessageStream)sender;
                    connection.Dispose();
                    Console.WriteLine("Client was disconnected");
                    Dispose(connection);
                    return;
                }

                Console.WriteLine($"{DateTime.Now}: Message arrived");
                foreach (var connection in connections)
                {
                    Task.Factory.StartNew(
                        state => connection.WriteMessageAsync((IMessage)state),
                        e.Result,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                }
            }
        }

        private void Dispose(SustainableMessageStream connection)
        {
            connections.TryRemove(connection);
            connection.MessageArrived -= OnMessageArrived;
            connection.ConnectionFailure -= OnWriteFailure;
            connection.Dispose();
        }
    }
}
