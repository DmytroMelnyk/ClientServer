namespace Networking.Client
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using ConnectionBehavior;
    using Core.AsyncEvents;
    using Core.Streams;
    using System.Threading;

    public class TcpClientImpl : IDisposable
    {
        private SustainableMessageStream _connection;
        private IConnectionBehavior _behavior;
        private IDisposable messageObservable;

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
            messageObservable = _connection.Messages.Subscribe(_behavior.OnMessage, ex => _behavior.OnException(this, ex));
        }

        public void AbortConnection()
        {
            Dispose();
        }

        public Task WriteMessageAsync(object message)
        {
            return _connection.WriteMessageAsync(message, CancellationToken.None);
        }

        public void Dispose()
        {
            _connection.Dispose();
            messageObservable.Dispose();
        }
    }
}
