using Networking.Messaging.ConnectionProcessors;
using Networking.Messaging.MessageProcessors;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.Messaging
{
    public class TcpClientImpl : IDisposable
    {
        private TcpMessagePipe connection;

        public TcpClientImpl()
        {
            connection = new TcpMessagePipe();
            connection.InvalidMessage += OnInvalidMessage;
            connection.MessageArrived += OnMessageArrived;
            connection.ReadFailure += OnReadFailure;
            connection.WriteFailure += OnWriteFailure;
        }

        private void OnWriteFailure(object sender, IMessage e)
        {
            ConnectAsync();
            WriteMessageAsync(e);
        }

        private void OnReadFailure(object sender, EventArgs e)
        {
            connection.ConnectAsync();
        }

        private void OnMessageArrived(object sender, IMessage e)
        {
            Console.WriteLine("Message arrived");
        }

        private void OnInvalidMessage(object sender, EventArgs e)
        {
            Dispose();
        }

        public Task ConnectAsync(IPEndPoint endpoint)
        {
            return connection.ConnectAsync(endpoint);
        }

        public Task WriteMessageAsync(IMessage message)
        {
            return connection.WriteMessageAsync(message);
        }

        public void StartReadingMessages()
        {
            connection.StartReadingMessages();
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
