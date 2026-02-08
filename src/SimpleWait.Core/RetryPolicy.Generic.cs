using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    /// <summary>
    /// Typed fluent retry/wait helper for operations that return <typeparamref name="TResult"/>.
    /// Mirrors the non-generic <see cref="RetryPolicy"/> API but returns a typed result.
    /// </summary>
    public class RetryPolicy<TResult>
    {
        private readonly DefaultWait<bool> wait;
        private static readonly Type DefaultException = typeof(TimeoutException);
        private Type exceptionType = DefaultException;

        /// <summary>
        /// Initializes a new instance of <see cref="RetryPolicy{TResult}"/> with a default timeout.
        /// </summary>
        public RetryPolicy()
        {
            this.wait = new DefaultWait<bool>(true) { Timeout = TimeSpan.FromSeconds(5) };
        }

        /// <summary>
        /// Configure exception types that should be ignored while waiting.
        /// </summary>
        public RetryPolicy<TResult> IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            this.wait.IgnoreExceptionTypes(exceptionTypes);
            return this;
        }

        /// <summary>
        /// Configure the retry policy to throw a specific exception type on timeout.
        /// </summary>
        public RetryPolicy<TResult> Throw<TException>() where TException : Exception
        {
            this.exceptionType = typeof(TException);
            return this;
        }

        /// <summary>
        /// Set the timeout for the wait.
        /// </summary>
        public RetryPolicy<TResult> Timeout(TimeSpan? timeout)
        {
            if (timeout.HasValue)
            {
                this.wait.Timeout = timeout.Value;
            }

            return this;
        }

        /// <summary>
        /// Set a custom message to include in timeout exceptions.
        /// </summary>
        public RetryPolicy<TResult> Message(string message)
        {
            this.wait.Message = message;
            return this;
        }

        /// <summary>
        /// Set the polling interval between condition evaluations.
        /// </summary>
        public RetryPolicy<TResult> PollingInterval(TimeSpan pollingInterval)
        {
            this.wait.PollingInterval = pollingInterval;
            return this;
        }

        /// <summary>
        /// Execute a synchronous condition that returns <typeparamref name="TResult"/>.
        /// Retries until a non-null result is returned or timeout elapses.
        /// </summary>
        public TResult Execute(Func<TResult> condition)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            TResult Func(bool _) => condition();

            try
            {
                return this.wait.Execute(Func);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
                throw;
            }
        }

        /// <summary>
        /// Execute an asynchronous condition that returns <typeparamref name="TResult"/>.
        /// Retries until a non-null result is returned or timeout elapses.
        /// </summary>
        public async Task<TResult> ExecuteAsync(Func<Task<TResult>> condition)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            try
            {
                return await this.wait.ExecuteAsync(condition).ConfigureAwait(false);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
                throw;
            }
        }

        /// <summary>
        /// Execute an asynchronous condition that returns <typeparamref name="TResult"/>, observing cancellation.
        /// </summary>
        public async Task<TResult> ExecuteAsync(Func<Task<TResult>> condition, CancellationToken token)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            try
            {
                return await this.wait.ExecuteAsync(condition, token).ConfigureAwait(false);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
                throw;
            }
        }

        /// <summary>
        /// Evaluate <paramref name="condition"/> repeatedly and apply <paramref name="success"/> to the result until success returns true
        /// or timeout elapses.
        /// </summary>
        public bool Success(Func<TResult> condition, Func<TResult, bool> success)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (success == null) throw new ArgumentNullException(nameof(success));

            try
            {
                _ = this.wait.Execute(_ =>
                {
                    var result = condition();
                    return success(result);
                });

                return true;
            }
            catch (Exception ex) when (ExceptionHelpers.IsTimeoutOrConfiguredTimeoutException(ex, this.exceptionType))
            {
                return false;
            }
        }
    }
}