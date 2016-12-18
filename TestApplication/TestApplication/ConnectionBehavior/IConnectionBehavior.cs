namespace Networking.Client.ConnectionBehavior
{
    using System;

    public interface IConnectionBehavior
    {
        void OnException(TcpClientImpl tcpClientImpl, Exception error);

        void OnMessage(object result);

        void OnConnectionFailure(TcpClientImpl tcpClientImpl);
    }
}