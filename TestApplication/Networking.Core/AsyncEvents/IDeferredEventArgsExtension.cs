namespace Networking.Core.AsyncEvents
{
    using System;
    using System.Threading.Tasks;

    public static class IDeferredEventArgsExtension
    {
        public static Task RaiseAsync<T>(this EventHandler<T> @this, object sender, T args)
            where T : IDeferredEventArgs
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var handler = @this;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            handler(sender, args);
            return args.WaitForDeferralsAsync();
        }
    }
}
