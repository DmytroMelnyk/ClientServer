namespace Networking.Core
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public class DefferedAsyncResultEventArgs<T> : AsyncCompletedEventArgs
    {
        private readonly DeferralManager _deferrals = new DeferralManager();

        private readonly T _result;

        public DefferedAsyncResultEventArgs(bool cancelled)
            : this(default(T), null, cancelled, null)
        {
        }

        public DefferedAsyncResultEventArgs(T result)
            : this(result, null, false, null)
        {
        }

        public DefferedAsyncResultEventArgs(Exception error)
            : this(default(T), error, false, null)
        {
        }

        private DefferedAsyncResultEventArgs(T result, Exception error, bool cancelled, object userState)
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

        internal Task WaitForDeferralsAsync()
        {
            return _deferrals.SignalAndWaitAsync();
        }
    }
}
