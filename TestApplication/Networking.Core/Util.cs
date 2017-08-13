using System.Reactive.Concurrency;

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

            using (var stream = new MemoryStream())
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

            using (var stream = new MemoryStream(binaryMessage))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }

        public static IObservable<Unit> ToPulsar(this IObservable<bool> operationStartStopNotifierHotObservable, TimeSpan period)
        {
            return operationStartStopNotifierHotObservable
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(Observable
                    .Interval(period)
                    .StartWith(0)
                    .TakeUntil(operationStartStopNotifierHotObservable.Where(on => !on).ObserveOn(new EventLoopScheduler())))
                .Select(_ => Unit.Default);
        }
    }
}
