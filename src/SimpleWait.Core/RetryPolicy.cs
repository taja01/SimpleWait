using System;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    public class RetryPolicy
    {
        private readonly DefaultWait<bool> wait;
        private static readonly Type DefaultException = typeof(TimeoutException);
        private Type exceptionType = DefaultException;
        public RetryPolicy()
        {
            this.wait = new DefaultWait<bool>(true) { Timeout = TimeSpan.FromSeconds(5) };
        }

        public static RetryPolicy Initialize()
        {
            return new RetryPolicy();
        }

        public RetryPolicy IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            this.wait.IgnoreExceptionTypes(exceptionTypes);
            return this;
        }

        public RetryPolicy Throw<T>() where T : Exception
        {
            this.exceptionType = typeof(T);
            return this;
        }

        public RetryPolicy Timeout(TimeSpan? timeout)
        {
            if (timeout.HasValue)
            {
                this.wait.Timeout = timeout.Value;
            }

            return this;
        }

        public RetryPolicy Message(string message)
        {
            this.wait.Message = message;
            return this;
        }

        public RetryPolicy PollingInterval(TimeSpan pollingInterval)
        {
            this.wait.PollingInterval = pollingInterval;
            return this;
        }

        public bool Success(Func<bool> condition)
        {
            try
            {
                this.Execute(condition);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        public void Execute(Func<bool> condition)
        {
            bool Func(bool b) => condition();
            try
            {
                _ = this.wait.Execute(Func);
            }
            catch (TimeoutException e) when (this.exceptionType != DefaultException)
            {
                throw (Exception)Activator.CreateInstance(this.exceptionType, e.Message);
            }
        }

        public void Execute<T>(Func<T> func, Func<T, bool> success, Func<T, string> message)
        {
            T t = default;
            try
            {
                _ = this.wait.Execute(_ =>
                {
                    t = func();
                    return success(t);
                });
            }
            catch (TimeoutException e) when (this.exceptionType == DefaultException)
            {
                throw (Exception)Activator.CreateInstance(this.exceptionType, $"{e.Message} | {message(t)}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public TResult Execute<TResult>(Func<TResult> condition)
        {
            TResult Func(bool b) => condition();
            try
            {
                return this.wait.Execute(Func);
            }
            catch (TimeoutException e) when (this.exceptionType != DefaultException)
            {
                throw (Exception)Activator.CreateInstance(this.exceptionType, e.Message);
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition)
        {
            try
            {
                return await wait.ExecuteAsync(condition);
            }
            catch (TimeoutException e) when (this.exceptionType != DefaultException)
            {
                throw (Exception)Activator.CreateInstance(this.exceptionType, e.Message);
            }
        }
    }
}