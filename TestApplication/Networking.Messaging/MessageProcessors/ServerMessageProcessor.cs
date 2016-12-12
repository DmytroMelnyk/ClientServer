using System;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Messaging.MessageProcessors
{
    public class ServerMessageProcessor : IMessageProcessor
    {
        public ServerMessageProcessor()
        {

        }

        public Task ProcessAsync(object message)
        {
            return Task.Factory.StartNew(Process, message, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void Process(object message)
        {
            if (message.GetType() == typeof(KeepAliveMessage))
            {
                Console.WriteLine("KeepAlive message received");
            }
            else
            {
                Console.WriteLine("Unknown message received");
            }
        }
    }
}
