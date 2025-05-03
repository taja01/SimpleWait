using System;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    public class AsyncRetryPolicy
    {
        private readonly AsyncDefaultWait<bool> wait;
        private static readonly Type DefaultException = typeof(TimeoutException);
        private Type exceptionType = DefaultException;

        public AsyncRetryPolicy()
        {
            this.wait = new AsyncDefaultWait<bool>(true) { Timeout = TimeSpan.FromSeconds(5) };
        }
        public static AsyncRetryPolicy Initialize()
        {
            return new AsyncRetryPolicy();
        }

        public AsyncRetryPolicy IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            this.wait.IgnoreExceptionTypes(exceptionTypes);
            return this;
        }

        public AsyncRetryPolicy Throw<T>() where T : Exception
        {
            this.exceptionType = typeof(T);
            return this;
        }

        public AsyncRetryPolicy Timeout(TimeSpan? timeout)
        {
            if (timeout.HasValue)
            {
                this.wait.Timeout = timeout.Value;
            }

            return this;
        }

        public AsyncRetryPolicy Message(string message)
        {
            this.wait.Message = message;
            return this;
        }

        public AsyncRetryPolicy PollingInterval(TimeSpan pollingInterval)
        {
            this.wait.PollingInterval = pollingInterval;
            return this;
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition)
        {
            try
            {
                return await wait.UntilAsync(condition);
            }
            catch (TimeoutException e) when (this.exceptionType != DefaultException)
            {
                throw (Exception)Activator.CreateInstance(this.exceptionType, e.Message);
            }
        }
    }
}
