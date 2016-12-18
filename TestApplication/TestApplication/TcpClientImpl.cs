namespace Networking.Client
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using ConnectionBehavior;
    using Core.AsyncEvents;
    using Core.Streams;

    public class TcpClientImpl : IDisposable
    {
        private SustainableMessageStream _connection;
        private IConnectionBehavior _behavior;

        public TcpClientImpl(IConnectionBehavior behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }

            _behavior = behavior;
        }

        public void EstablishConnection(IPEndPoint endPoint)
        {
            var client = new TcpClient();
            client.Connect(endPoint.Address, endPoint.Port);
            var networkStream = client.GetStream();
            _connection = new SustainableMessageStream(networkStream);
            _connection.ConnectionFailure += OnConnectionFailure;
            _connection.MessageArrived += OnMessageArrived;
            _connection.StartReadingMessages();
        }

        public void AbortConnection()
        {
            Dispose();
        }

        public Task WriteMessageAsync(object message)
        {
            return _connection.WriteMessageAsync(message);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private void OnMessageArrived(object sender, DeferredAsyncResultEventArgs<object> e)
        {
            using (e.GetDeferral())
            {
                if (e.Error != null)
                {
                    _behavior.OnException(this, e.Error);
                }
                else
                {
                    _behavior.OnMessage(e.Result);
                }
            }
        }

        private void OnConnectionFailure(object sender, DeferredAsyncCompletedEventArgs e)
        {
            using (e.GetDeferral())
            {
                _behavior.OnConnectionFailure(this);
            }
        }
    }
}
