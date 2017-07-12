namespace Networking.Server
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using ConnectionBehavior;
    using Core.AsyncEvents;
    using Core.Streams;

    public class TcpServerImpl : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly IConnectionsBehavior _behavior;

        public TcpServerImpl(IPEndPoint endpoint, IConnectionsBehavior behavior)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            _listener = new TcpListener(endpoint);
            _behavior = behavior;
        }

        public void StartListening()
        {
            _listener.Start();
            while (true)
            {
                var tcpClient = _listener.AcceptTcpClient();
                var connection = new SustainableMessageStream(tcpClient.GetStream());
                connection.Messages.Subscribe(message => _behavior.OnMessage(connection, message), ex => _behavior.OnException(connection, ex));
                _behavior.OnNewConnectionArrived(connection);
            }
        }

        public void Dispose()
        {
            _behavior.Dispose();

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
