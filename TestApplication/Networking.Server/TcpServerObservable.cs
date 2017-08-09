namespace Networking.Server
{
    using System;
    using System.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using Networking.Core;

    public class TcpServerObservable : IDisposable
    {
        private readonly ConcurrentSet<SustainableMessageStream> _connections;
        private readonly IDisposable _token;

        public TcpServerObservable(TcpListenerObservable listener)
        {
            _connections = new ConcurrentSet<SustainableMessageStream>();
            var listenerObservable = listener ?? throw new ArgumentNullException(nameof(listener));
            _token = listenerObservable.Subscribe(OnNewConnectionArrived);
            listenerObservable.Connect();
        }

        public void Dispose()
        {
            _token.Dispose();
            foreach (var connection in _connections)
            {
                connection.Dispose();
            }
        }

        private void OnException(SustainableMessageStream connection, Exception error)
        {
            if (!_connections.TryRemove(connection))
            {
                return;
            }

            Console.WriteLine(error.Message);
            Console.WriteLine("Client was disconnected");
        }

        private void OnMessage(SustainableMessageStream connection, object result)
        {
            Console.WriteLine($"{DateTime.Now}: Message arrived");
            foreach (var con in _connections.Where(x => x != connection))
            {
                con.WriteMessageAsync(result, CancellationToken.None)
                    .ToObservable()
                    .Subscribe(
                        x => { },
                        ex => OnException(con, ex));
            }
        }

        private void OnNewConnectionArrived(SustainableMessageStream connection)
        {
            if (!_connections.TryAdd(connection))
            {
                return;
            }

            Console.WriteLine("New connection was added");
            connection.Messages
                .Subscribe(
                    message => OnMessage(connection, message),
                    ex => OnException(connection, ex));
        }
    }
}
