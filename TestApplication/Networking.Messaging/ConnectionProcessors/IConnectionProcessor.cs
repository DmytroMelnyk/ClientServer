namespace Networking.Messaging.ConnectionProcessors
{
    public interface IConnectionProcessor
    {
        void OnWriteFailure(TcpMessagePipe tcpMessagePipe, IMessage message);
        void OnReadFailure(TcpMessagePipe tcpMessagePipe);
        void OnInvalidMessage(TcpMessagePipe tcpMessagePipe);
    }
}