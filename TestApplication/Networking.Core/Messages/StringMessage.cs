namespace Networking.Core.Messages
{
    using System;

    [Serializable]
    public sealed class StringMessage : IMessage
    {
        public string Message { get; set; }
    }
}
