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
            catch (Exception ex) when (IsTimeoutOrConfiguredTimeoutException(ex))
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
            catch (TimeoutException e)
            {
                ThrowConfiguredOrDefault(e);
                throw;
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition)
        {
            try
            {
                return await wait.ExecuteAsync(condition);
            }
            catch (TimeoutException e)
            {
                ThrowConfiguredOrDefault(e);
                throw;
            }
        }

        private bool IsTimeoutOrConfiguredTimeoutException(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (this.exceptionType != DefaultException)
            {
                return this.exceptionType.IsAssignableFrom(ex.GetType());
            }
            return false;
        }

        private void ThrowConfiguredOrDefault(TimeoutException timeoutEx, string overrideMessage = null)
        {
            var message = overrideMessage ?? timeoutEx.Message;

            if (this.exceptionType != DefaultException)
            {
                Exception created = null;

                try
                {
                    created = (Exception)Activator.CreateInstance(this.exceptionType, message, timeoutEx);
                }
                catch { /* ignore and try other ctors */ }

                if (created == null)
                {
                    try
                    {
                        created = (Exception)Activator.CreateInstance(this.exceptionType, message);
                    }
                    catch { }
                }
                if (created == null)
                {
                    try
                    {
                        created = (Exception)Activator.CreateInstance(this.exceptionType);
                    }
                    catch { }
                }

                if (created != null)
                {
                    throw created;
                }

                // If we couldn't construct the configured exception type, fall back to InvalidOperationException
                throw new InvalidOperationException($"Failed to create exception of type {this.exceptionType.FullName}", timeoutEx);
            }
            else
            {
                throw new TimeoutException(message, timeoutEx);
            }
        }
    }
}