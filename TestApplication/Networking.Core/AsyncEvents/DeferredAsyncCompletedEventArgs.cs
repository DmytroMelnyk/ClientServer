namespace Networking.Core.AsyncEvents
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public class DeferredAsyncCompletedEventArgs : AsyncCompletedEventArgs, IDeferredEventArgs
    {
        private readonly DeferralManager _deferrals = new DeferralManager();

        public DeferredAsyncCompletedEventArgs(Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
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
