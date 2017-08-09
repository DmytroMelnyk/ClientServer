﻿namespace Networking.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly Task<IDisposable> _releaser;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _releaser = Task.FromResult<IDisposable>(new Releaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            var wait = _semaphore.WaitAsync();

            return wait.IsCompleted ? _releaser :
                wait.ContinueWith((_, state) => (IDisposable)state, _releaser.Result, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;

            internal Releaser(AsyncLock toRelease) => _toRelease = toRelease;

            public void Dispose() => _toRelease._semaphore.Release();
        }
    }
}