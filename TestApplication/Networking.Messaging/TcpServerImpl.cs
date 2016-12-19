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
                connection.MessageArrived += OnMessageArrived;
                connection.ConnectionFailure += OnConnectionFailure;
                connection.StartReadingMessages();
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

        private void OnConnectionFailure(object sender, DeferredAsyncCompletedEventArgs e)
        {
            using (e.GetDeferral())
            {
                var connection = (SustainableMessageStream)sender;
                Dispose(connection);
                _behavior.OnConnectionFailure(connection);
            }
        }

        private void OnMessageArrived(object sender, DeferredAsyncResultEventArgs<object> e)
        {
            using (e.GetDeferral())
            {
                var connection = (SustainableMessageStream)sender;

                if (e.Error != null)
                {
                    Dispose(connection);
                    _behavior.OnConnectionFailure(connection);
                    return;
                }

                _behavior.OnMessage(connection, e.Result);
            }
        }

        private void Dispose(SustainableMessageStream connection)
        {
            connection.MessageArrived -= OnMessageArrived;
            connection.ConnectionFailure -= OnConnectionFailure;
            connection.Dispose();
        }
    }
}
