namespace Networking.Client
{
    using System;
    using System.IO;
    using System.Net;
    using Core.Messages;

    public class DefaultConnectionBehaviour : IConnectionBehavior
    {
        private readonly IPEndPoint[] _availableServers;
        private int _currentServerIPEndPointIndex;

        public DefaultConnectionBehaviour(params IPEndPoint[] availableServers)
        {
            if (availableServers == null)
            {
                throw new ArgumentNullException(nameof(availableServers));
            }

            if (availableServers.Length == 0)
            {
                throw new ArgumentException("Should not be empty", nameof(availableServers));
            }

            this._availableServers = availableServers;
            _currentServerIPEndPointIndex = 0;
        }

        public IPEndPoint CurrentServerIPEndPoint
        {
            get { return _availableServers[_currentServerIPEndPointIndex]; }
        }

        public void OnConnectionFailure(TcpClientImpl tcpClientImpl)
        {
            tcpClientImpl.AbortConnection();

            try
            {
                _currentServerIPEndPointIndex++;
                if (_currentServerIPEndPointIndex >= _availableServers.Length)
                {
                    throw new ApplicationException("There is no accessible IPEndPoints.");
                }

                tcpClientImpl.EstablishConnection(CurrentServerIPEndPoint);
            }
            catch (Exception ex)
                when (ex.GetType() != typeof(ApplicationException))
            {
            }
        }

        public void OnException(TcpClientImpl tcpClientImpl, Exception error)
        {
            Console.WriteLine(error.Message);

            if (error.GetType() != typeof(InvalidDataException))
            {
                OnConnectionFailure(tcpClientImpl);
            }
        }

        public void OnMessage(IMessage result)
        {
            if (result is StringMessage)
            {
                Console.WriteLine((result as StringMessage).Message);
            }
        }
    }
}
