namespace Networking.Client
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;

    public class TcpClientObservable
    {
        private SustainableMessageStream _sustainableMessageStream;

        public IObservable<object> Messages { get; }

        public TcpClientObservable(params IPEndPoint[] ipEndPoints)
        {
            Messages = Observable
                .OnErrorResumeNext(ipEndPoints.Select(GetConnection))
                .Select(x => x);
        }

        private IObservable<object> GetConnection(IPEndPoint endPoint)
        {
            return Observable.Create<object>(observer =>
            {
                var client = new TcpClient();
                client.Connect(endPoint.Address, endPoint.Port);
                var networkStream = new SustainablePacketStream(client.GetStream(), TimeSpan.FromSeconds(5));
                _sustainableMessageStream = new SustainableMessageStream(networkStream);
                return new CompositeDisposable(_sustainableMessageStream.Messages.Subscribe(observer), client);
            });
        }

        public Task WriteMessageAsync(object message, CancellationToken ct)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _sustainableMessageStream.WriteMessageAsync(message, ct);
        }
    }
}
