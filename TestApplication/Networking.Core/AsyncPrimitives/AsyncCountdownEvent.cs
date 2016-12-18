namespace Networking.Core.AsyncPrimitives
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class AsyncCountdownEvent
    {
        private readonly TaskCompletionSource<object> _tcs;

        private int _count;

        public AsyncCountdownEvent(int count)
        {
            _tcs = new TaskCompletionSource<object>();
            _count = count;
        }

        public int CurrentCount
        {
            get { return Volatile.Read(ref _count); }
        }

        public Task WaitAsync()
        {
            return _tcs.Task;
        }

        public void AddCount(int signalCount = 1)
        {
            if (!ModifyCount(signalCount))
            {
                throw new InvalidOperationException("Cannot increment count.");
            }
        }

        public void Signal(int signalCount = 1)
        {
            if (!ModifyCount(-signalCount))
            {
                throw new InvalidOperationException("Cannot decrement count.");
            }
        }

        private bool ModifyCount(int signalCount)
        {
            while (true)
            {
                int oldCount = CurrentCount;

                if (oldCount == 0)
                {
                    return false;
                }

                int newCount = oldCount + signalCount;

                if (newCount < 0)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _count, newCount, oldCount) == oldCount)
                {
                    if (newCount == 0)
                    {
                        _tcs.SetResult(null);
                    }

                    return true;
                }
            }
        }
    }
}
