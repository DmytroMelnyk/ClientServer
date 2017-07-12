namespace Networking.Server.ConnectionBehavior
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Streams;

    public class SimpleConnectionBehaviour : IConnectionsBehavior
    {
        private ConcurrentSet<SustainableMessageStream> connections = new ConcurrentSet<SustainableMessageStream>();

        public void OnConnectionFailure(SustainableMessageStream connection)
        {
            if (connections.TryRemove(connection))
            {
                Console.WriteLine("Client was disconnected");
            }
        }

        public void OnException(SustainableMessageStream connection, Exception error)
        {
            Console.WriteLine(error.Message);
            if (error.GetType() != typeof(InvalidDataException))
            {
                OnConnectionFailure(connection);
            }
        }

        public void OnMessage(SustainableMessageStream connection, object result)
        {
            Console.WriteLine($"{DateTime.Now}: Message arrived");
            foreach (var con in connections)
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

        public void OnNewConnectionArrived(SustainableMessageStream connection)
        {
            if (connections.TryAdd(connection))
            {
                Console.WriteLine("New connection was added");
            }
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
