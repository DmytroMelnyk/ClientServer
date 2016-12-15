using System;
using System.Net;
using System.Threading.Tasks;
using Networking.Core;
using System.ComponentModel;

namespace Networking.Client
{
    public class TcpClientImpl : IDisposable
    {
        private readonly TcpMessagePipe connection;
        private readonly IPEndPoint[] availableServers;

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

            this.availableServers = availableServers;
            currentServerIPEndPointIndex = -1;
            connection = new TcpMessagePipe();
            connection.MessageArrived += OnMessageArrived;
            connection.ConnectionFailure += OnConnectionFailure;
        }

        private async void OnConnectionFailure(object sender, DefferedAsyncCompletedEventArgs e)
        {
            using (e.GetDeferral())
            {
                await ConnectAsync().ConfigureAwait(false);
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                currentServerIPEndPointIndex++;
                if (currentServerIPEndPointIndex >= availableServers.Length)
                    throw new ApplicationException("There is no accessible IPEndPoints.");
                await ConnectAsync(CurrentServerIPEndPoint).ConfigureAwait(false);
            }
            catch (Exception ex) 
                when (ex.GetType() != typeof(ApplicationException))
            {
            }
        }

        private async void OnMessageArrived(object sender, DefferedAsyncResultEventArgs<IMessage> e)
        {
            using (e.GetDeferral())
            {
                if (e.Cancelled)
                {
                    Console.WriteLine("Reading was cancelled");
                    return;
                }
                if (e.Error != null)
                {
                    connection.StopReading();
                    await ConnectAsync().ConfigureAwait(false);
                    StartReadingMessagesAsync();
                    return;
                }

                Console.WriteLine(e.Result);
            }
        }

        private Task ConnectAsync(IPEndPoint endpoint)
        {
            return connection.ConnectAsync(endpoint);
        }

        public Task WriteMessageAsync(IMessage message)
        {
            return connection.WriteMessageAsync(message);
        }

        public void StartReadingMessagesAsync()
        {
            connection.StartReadingMessagesAsync();
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
