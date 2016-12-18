namespace Networking.Core.AsyncEvents
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public class DeferredAsyncResultEventArgs<T> : AsyncCompletedEventArgs, IDeferredEventArgs
    {
        private readonly DeferralManager _deferrals = new DeferralManager();

        private readonly T _result;

        public DeferredAsyncResultEventArgs(bool cancelled)
            : this(default(T), null, cancelled, null)
        {
        }

        public DeferredAsyncResultEventArgs(T result)
            : this(result, null, false, null)
        {
        }

        public DeferredAsyncResultEventArgs(Exception error)
            : this(default(T), error, false, null)
        {
        }

        private DeferredAsyncResultEventArgs(T result, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _result = result;
        }

        public T Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _result;
            }
        }

        public IDisposable GetDeferral()
        {
            return _deferrals.GetDeferral();
        }

        Task IDeferredEventArgs.WaitForDeferralsAsync()
        {
            return _deferrals.SignalAndWaitAsync();
        }
    }
}
