namespace Networking.Core.Utils
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Messages;

    internal static class Util
    {
        public static byte[] ZeroLengthPacket { get; } = new byte[0];

        public static byte[] ToPacket(this IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, message);
                return stream.ToArray();
            }
        }

        public static IMessage ToMessage(this byte[] binaryMessage)
        {
            if (binaryMessage == null)
            {
                return null;
            }

            using (MemoryStream stream = new MemoryStream(binaryMessage))
            {
                return (IMessage)new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
