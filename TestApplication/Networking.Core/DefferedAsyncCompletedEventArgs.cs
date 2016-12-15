using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Core
{
    public class DefferedAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        private readonly DeferralManager _deferrals = new DeferralManager();

        public DefferedAsyncCompletedEventArgs(Exception error, bool cancelled, object userState) :
                    base(error, cancelled, userState)
        {
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
