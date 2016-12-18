namespace Networking.Core.Utils
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    internal static class Util
    {
        public static byte[] ZeroLengthPacket { get; } = new byte[0];

        public static byte[] ToPacket(this object message)
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

        public static object ToMessage(this byte[] binaryMessage)
        {
            if (binaryMessage == null)
            {
                return null;
            }

            using (MemoryStream stream = new MemoryStream(binaryMessage))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
