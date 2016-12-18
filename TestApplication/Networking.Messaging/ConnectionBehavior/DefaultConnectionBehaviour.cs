namespace Networking.Server.ConnectionBehavior
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Messages;
    using Core.Streams;

    public class DefaultConnectionBehaviour : IConnectionsBehavior
    {
        private ConcurrentSet<SustainableMessageStream> connections = new ConcurrentSet<SustainableMessageStream>();

        public void OnConnectionFailure(SustainableMessageStream connection)
        {
            connections.TryRemove(connection);
            Console.WriteLine("Client was disconnected");
        }

        public void OnException(SustainableMessageStream connection, Exception error)
        {
            Console.WriteLine(error.Message);
            if (error.GetType() != typeof(InvalidDataException))
            {
                OnConnectionFailure(connection);
            }
        }

        public void OnMessage(SustainableMessageStream connection, IMessage result)
        {
            Console.WriteLine($"{DateTime.Now}: Message arrived");
            foreach (var con in connections)
            {
                Task.Factory.StartNew(
                    state => con.WriteMessageAsync((IMessage)state),
                    result,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
        }

        public void OnNewConnectionArrived(SustainableMessageStream connection)
        {
            connections.TryAdd(connection);
            Console.WriteLine("New connection was added");
        }

        public void Dispose()
        {
            foreach (var connection in connections)
            {
                connection.Dispose();
            }
        }
    }
}
