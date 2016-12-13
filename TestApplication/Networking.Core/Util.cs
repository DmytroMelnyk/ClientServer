﻿using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Networking.Core
{
    public static class Util
    {
        public static byte[] ToPacket(this IMessage message)
        {
            if (message.GetType() == typeof(KeepAliveMessage))
                return new byte[0];

            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, message);
                return stream.ToArray();
            }
        }

        public static IMessage ToMessage(this byte[] binaryMessage)
        {
            if (binaryMessage.Length == 0)
                return new KeepAliveMessage();

            using (MemoryStream stream = new MemoryStream(binaryMessage))
                return (IMessage)new BinaryFormatter().Deserialize(stream);
        }
    }
}