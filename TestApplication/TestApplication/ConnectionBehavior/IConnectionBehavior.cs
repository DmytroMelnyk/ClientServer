namespace Networking.Client.ConnectionBehavior
{
    using System;
    using Core.Messages;

    public interface IConnectionBehavior
    {
        void OnException(TcpClientImpl tcpClientImpl, Exception error);

        void OnMessage(IMessage result);

        void OnConnectionFailure(TcpClientImpl tcpClientImpl);
    }
}