namespace Networking.Server
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Streams;

    public class TcpServerImpl : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentSet<SustainableMessageStream> _connections = new ConcurrentSet<SustainableMessageStream>();

        private void OnConnectionFailure(SustainableMessageStream connection)
        {
            if (_connections.TryRemove(connection))
            {
                Console.WriteLine("Client was disconnected");
            }
        }

        private void OnException(SustainableMessageStream connection, Exception error)
        {
            Console.WriteLine(error.Message);
            if (error.GetType() != typeof(InvalidDataException))
            {
                OnConnectionFailure(connection);
            }
        }

        private void OnMessage(SustainableMessageStream connection, object result)
        {
            Console.WriteLine($"{DateTime.Now}: Message arrived");
            foreach (var con in _connections)
            {
                if (con == connection)
                {
                    continue;
                }

                Task.Factory.StartNew(
                    state => con.WriteMessageAsync(state, CancellationToken.None),
                    result,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
        }

        private void OnNewConnectionArrived(SustainableMessageStream connection)
        {
            if (_connections.TryAdd(connection))
            {
                Console.WriteLine("New connection was added");
            }
        }

        public TcpServerImpl(IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            _listener = new TcpListener(endpoint);
        }

        public void StartListening()
        {
            _listener.Start();
            while (true)
            {
                var tcpClient = _listener.AcceptTcpClient();
                var connection = new SustainableMessageStream(new SustainablePacketStream(tcpClient.GetStream(), TimeSpan.FromSeconds(5)));
                connection.Messages.Subscribe(message => OnMessage(connection, message), ex => OnException(connection, ex));
                OnNewConnectionArrived(connection);
            }
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
            {
                connection.Dispose();
            }

            try
            {
                _listener.Stop();
            }
            catch (SocketException)
            {
            }
        }
    }
}
