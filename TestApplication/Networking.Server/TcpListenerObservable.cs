namespace Networking.Server
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Networking.Core;

    public class TcpListenerObservable : IConnectableObservable<SustainableMessageStream>
    {
        private readonly IConnectableObservable<SustainableMessageStream> _connectedStreams;

        public TcpListenerObservable(IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            _connectedStreams = Observable.Create<SustainableMessageStream>(observer =>
                {
                    var listener = new TcpListener(endpoint);
                    listener.Start();

                    var token = Observable
                        .FromAsync(() => listener.AcceptTcpClientAsync(), Scheduler.CurrentThread)
                        .Repeat()
                        .Select(tcpClient => new SustainablePacketStream(tcpClient.GetStream(), TimeSpan.FromSeconds(5)))
                        .Select(x => new SustainableMessageStream(x))
                        .Subscribe(observer);

                    return () =>
                    {
                        token.Dispose();
                        try
                        {
                            listener.Stop();
                        }
                        catch (SocketException)
                        {
                        }
                    };
                })
                .Publish();
        }

        public IDisposable Subscribe(IObserver<SustainableMessageStream> observer) => _connectedStreams.Subscribe(observer);

        public IDisposable Connect() => _connectedStreams.Connect();
    }
}
