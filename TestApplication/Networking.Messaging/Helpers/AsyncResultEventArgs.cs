using System;
using System.ComponentModel;

namespace Networking.Messaging.Helpers
{
    public class AsyncResultEventArgs<T> : AsyncCompletedEventArgs
    {
        T result;

        private AsyncResultEventArgs(T result_, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            result = result_;
        }

        public AsyncResultEventArgs(bool cancelled)
            : this(default(T), null, cancelled, null)
        {
        }

        public AsyncResultEventArgs(T result_)
            : this(result_, null, false, null)
        {
        }

        public AsyncResultEventArgs(Exception error)
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
    }
}
