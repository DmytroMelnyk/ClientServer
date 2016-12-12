using System;

namespace Networking.Messaging
{
    public interface IMessage
    {
    }

    [Serializable]
    public sealed class KeepAliveMessage : IMessage
    {
        public static KeepAliveMessage Instance { get; } = new KeepAliveMessage();
    }
}
