﻿namespace Networking.Core.AsyncEvents
{
    using System;
    using System.Threading.Tasks;
    using AsyncPrimitives;

    public sealed class DeferralManager
    {
        private readonly AsyncCountdownEvent _count = new AsyncCountdownEvent(1);

        public IDisposable GetDeferral()
        {
            return new Deferral(_count);
        }

        public Task SignalAndWaitAsync()
        {
            _count.Signal();
            return _count.WaitAsync();
        }

        private sealed class Deferral : IDisposable
        {
            private AsyncCountdownEvent _count;

            public Deferral(AsyncCountdownEvent count)
            {
                _count = count;
                _count.AddCount();
            }

            void IDisposable.Dispose()
            {
                if (_count == null)
                {
                    return;
                }

                _count.Signal();
                _count = null;
            }
        }
    }
}
