namespace Networking.Server
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using ConnectionBehavior;
    using Core.AsyncEvents;
    using Core.Messages;
    using Core.Streams;

    public class TcpServerImpl : IDisposable
    {
        private TcpListener listener;
        private IConnectionsBehavior _behavior;

        public TcpServerImpl(IPEndPoint endpoint, IConnectionsBehavior behavior)
        {
            listener = new TcpListener(endpoint);
        }

        public void StartListening()
        {
            listener.Start();
            while (true)
            {
                var tcpClient = listener.AcceptTcpClient();
                var connection = new SustainableMessageStream(tcpClient.GetStream());
                connection.MessageArrived += OnMessageArrived;
                connection.ConnectionFailure += OnWriteFailure;
                connection.StartReadingMessages();
                _behavior.OnNewConnectionArrived(connection);
            }
        }

        public void Dispose()
        {
            _behavior.Dispose();
            listener.Stop();
        }

        private void OnWriteFailure(object sender, DeferredAsyncCompletedEventArgs e)
        {
            using (e.GetDeferral())
            {
                var connection = (SustainableMessageStream)sender;
                connection.MessageArrived -= OnMessageArrived;
                connection.ConnectionFailure -= OnWriteFailure;
                connection.Dispose();
                _behavior.OnConnectionFailure(connection);
            }
        }

        private void OnMessageArrived(object sender, DeferredAsyncResultEventArgs<IMessage> e)
        {
            using (e.GetDeferral())
            {
                if (e.Error != null)
                {
                    _behavior.OnException((SustainableMessageStream)sender, e.Error);
                    return;
                }

                _behavior.OnMessage((SustainableMessageStream)sender, e.Result);
            }
        }
    }
}
