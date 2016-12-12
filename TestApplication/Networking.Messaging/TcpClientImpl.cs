using Networking.Messaging.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Networking.Messaging
{
    public class TcpClientImpl : IDisposable
    {
        private TcpMessagePipe connection;
        private IPEndPoint[] availableServers;

        private int currentServerIPEndPointIndex;
        public IPEndPoint CurrentServerIPEndPoint
        {
            get { return availableServers[currentServerIPEndPointIndex]; }
        }

        public TcpClientImpl(params IPEndPoint[] availableServers)
        {
            if (availableServers == null)
                throw new ArgumentNullException(nameof(availableServers));
            if (availableServers.Length == 0)
                throw new ArgumentException("Should not be empty", nameof(availableServers));

            this.currentServerIPEndPointIndex = 0;
            connection = new TcpMessagePipe();
            connection.MessageArrived += OnMessageArrived;
            connection.WriteFailure += OnWriteFailure;
        }

        private async void OnWriteFailure(object sender, IMessage e)
        {
            await ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            try
            {
                currentServerIPEndPointIndex++;
                if (currentServerIPEndPointIndex != availableServers.Length)
                    await ConnectAsync(CurrentServerIPEndPoint);
            }
            catch
            {
            }
        }

        private async void OnMessageArrived(object sender, AsyncResultEventArgs<IMessage> e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine("Reading was cancelled");
                return;
            }
            if (e.Error != null)
            {
                await connection.StopReadingAsync();
                await ReconnectAsync();
                StartReadingMessages();
                return;
            }

            Console.WriteLine(e.Result);
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
