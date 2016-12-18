namespace Networking.Server.ConnectionBehavior
{
    using System;
    using Core.Messages;
    using Core.Streams;

    public interface IConnectionsBehavior : IDisposable
    {
        void OnNewConnectionArrived(SustainableMessageStream connection);

        void OnException(SustainableMessageStream connection, Exception error);

        void OnMessage(SustainableMessageStream connection, IMessage message);

        void OnConnectionFailure(SustainableMessageStream connection);
    }
}