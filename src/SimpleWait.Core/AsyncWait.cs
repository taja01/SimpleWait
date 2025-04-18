using System;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    public class AsyncWait
    {
        private readonly AsyncDefaultWait<bool> wait;
        private static readonly Type DefaultException = typeof(TimeoutException);
        private Type exceptionType = DefaultException;

        public AsyncWait()
        {
            this.wait = new AsyncDefaultWait<bool>(true) { Timeout = TimeSpan.FromSeconds(5) };
        }
        public static AsyncWait Initialize()
        {
            return new AsyncWait();
        }

        public AsyncWait IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            this.wait.IgnoreExceptionTypes(exceptionTypes);
            return this;
        }

        public AsyncWait Throw<T>() where T : Exception
        {
            this.exceptionType = typeof(T);
            return this;
        }

        public AsyncWait Timeout(TimeSpan? timeout)
        {
            if (timeout.HasValue)
            {
                this.wait.Timeout = timeout.Value;
            }

            return this;
        }

        public AsyncWait Message(string message)
        {
            this.wait.Message = message;
            return this;
        }

        public AsyncWait PollingInterval(TimeSpan pollingInterval)
        {
            this.wait.PollingInterval = pollingInterval;
            return this;
        }

        public async Task<TResult> UntilAsync<TResult>(Func<Task<TResult>> condition)
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
