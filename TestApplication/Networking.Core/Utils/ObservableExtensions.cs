using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Networking.Core.Utils
{
    public static class ObservableExtensions
    {
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