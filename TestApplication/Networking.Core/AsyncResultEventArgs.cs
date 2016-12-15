using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Networking.Core
{
    public class DefferedAsyncResultEventArgs<T> : AsyncCompletedEventArgs
    {
        private readonly DeferralManager _deferrals = new DeferralManager();

        T result;

        private DefferedAsyncResultEventArgs(T result, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            this.result = result;
        }

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

        public T Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return result;
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
