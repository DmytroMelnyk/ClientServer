namespace Networking.Core.AsyncEvents
{
    using System;
    using System.Threading.Tasks;

    public interface IDeferredEventArgs
    {
        IDisposable GetDeferral();

        Task WaitForDeferralsAsync();
    }
}
