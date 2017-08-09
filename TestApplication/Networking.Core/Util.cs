namespace Networking.Core
{
    using System;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Linq;
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

        public static IObservable<Unit> ToPulsar(this IObservable<bool> operationStartStopNotifier, TimeSpan period)
        {
            return operationStartStopNotifier
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(Observable
                    .Interval(period)
                    .StartWith(0) // starts timer immediately
                    .TakeUntil(operationStartStopNotifier.Where(on => !on)))
                .Select(_ => Unit.Default);
        }
    }
}
