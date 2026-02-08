using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    /// <summary>
    /// Fluent helper that wraps a <see cref="DefaultWait{T}"/> to provide retry/wait policies
    /// for synchronous and asynchronous operations.
    /// </summary>
    public partial class RetryPolicy
    {
        private readonly DefaultWait<bool> wait;
        private static readonly Type DefaultException = typeof(TimeoutException);
        private Type exceptionType = DefaultException;

        /// <summary>
        /// Create a typed retry policy for <typeparamref name="TResult"/>.
        /// </summary>
        public static RetryPolicy<TResult> For<TResult>() => new RetryPolicy<TResult>();

        /// <summary>
        /// Initializes a new instance of <see cref="RetryPolicy"/> with a default timeout.
        /// </summary>
        public RetryPolicy()
        {
            this.wait = new DefaultWait<bool>(true) { Timeout = TimeSpan.FromSeconds(5) };
        }

        /// <summary>
        /// Creates a new <see cref="RetryPolicy"/> instance.
        /// </summary>
        /// <returns>A new <see cref="RetryPolicy"/>.</returns>
        public static RetryPolicy Initialize()
        {
            return new RetryPolicy();
        }

        /// <summary>
        /// Configure exception types that should be ignored while waiting.
        /// </summary>
        /// <param name="exceptionTypes">Exception types to ignore during retries.</param>
        /// <returns>The same <see cref="RetryPolicy"/> instance for chaining.</returns>
        public RetryPolicy IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            this.wait.IgnoreExceptionTypes(exceptionTypes);
            return this;
        }

        /// <summary>
        /// Configure the retry policy to throw a specific exception type on timeout.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Exception"/> to throw on timeout.</typeparam>
        /// <returns>The same <see cref="RetryPolicy"/> instance for chaining.</returns>
        public RetryPolicy Throw<T>() where T : Exception
        {
            this.exceptionType = typeof(T);
            return this;
        }

        /// <summary>
        /// Set the timeout for the wait.
        /// </summary>
        /// <param name="timeout">The timeout to apply. If null, current value is unchanged.</param>
        /// <returns>The same <see cref="RetryPolicy"/> instance for chaining.</returns>
        public RetryPolicy Timeout(TimeSpan? timeout)
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
        /// <param name="message">Message appended to timeout exception messages.</param>
        /// <returns>The same <see cref="RetryPolicy"/> instance for chaining.</returns>
        public RetryPolicy Message(string message)
        {
            this.wait.Message = message;
            return this;
        }

        /// <summary>
        /// Set the polling interval between condition evaluations.
        /// </summary>
        /// <param name="pollingInterval">Polling interval.</param>
        /// <returns>The same <see cref="RetryPolicy"/> instance for chaining.</returns>
        public RetryPolicy PollingInterval(TimeSpan pollingInterval)
        {
            this.wait.PollingInterval = pollingInterval;
            return this;
        }

        /// <summary>
        /// Evaluate the boolean <paramref name="condition"/> until it returns true or the timeout elapses.
        /// </summary>
        /// <param name="condition">Condition to evaluate repeatedly.</param>
        /// <returns>True if the condition became true; false if timeout occurred.</returns>
        public bool Success(Func<bool> condition)
        {
            try
            {
                this.Execute(condition);
                return true;
            }
            catch (Exception ex) when (ExceptionHelpers.IsTimeoutOrConfiguredTimeoutException(ex, this.exceptionType))
            {
                return false;
            }
        }

        /// <summary>
        /// Evaluate the boolean <paramref name="condition"/> until it returns true or the timeout elapses,
        /// supporting cancellation via <paramref name="token"/>.
        /// </summary>
        /// <param name="condition">Condition to evaluate repeatedly.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the condition became true; false if timeout occurred.</returns>
        public bool Success(Func<bool> condition, CancellationToken token)
        {
            try
            {
                this.Execute(condition, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ExceptionHelpers.IsTimeoutOrConfiguredTimeoutException(ex, this.exceptionType))
            {
                return false;
            }
        }

        /// <summary>
        /// Execute the boolean <paramref name="condition"/> until it returns true or timeout.
        /// If a custom exception type was configured via <see cref="Throw{T}"/>, it will be thrown on timeout.
        /// </summary>
        /// <param name="condition">Condition delegate.</param>
        public void Execute(Func<bool> condition)
        {
            bool Func(bool b) => condition();
            try
            {
                _ = this.wait.Execute(Func);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
            }
        }

        /// <summary>
        /// Execute the boolean <paramref name="condition"/> until it returns true or timeout, observing <paramref name="token"/>.
        /// </summary>
        /// <param name="condition">Condition delegate.</param>
        /// <param name="token">Cancellation token to cancel wait.</param>
        public void Execute(Func<bool> condition, CancellationToken token)
        {
            bool Func(bool b) => condition();
            try
            {
                _ = this.wait.Execute(Func, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
            }
        }

        /// <summary>
        /// Execute a typed function repeatedly until the <paramref name="success"/> predicate returns true
        /// or timeout occurs. When a timeout occurs the configured exception message will include the last result via <paramref name="message"/>.
        /// </summary>
        /// <typeparam name="T">Type returned by <paramref name="func"/>.</typeparam>
        /// <param name="func">Function to execute on each iteration.</param>
        /// <param name="success">Predicate that determines whether the result is considered successful.</param>
        /// <param name="message">Function to obtain a diagnostic message from the last result.</param>
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
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e, $"{e.Message} | {message(t)}");
            }
        }

        /// <summary>
        /// Execute a synchronous condition that returns a result object. The call will retry until a non-null
        /// result is returned or the timeout elapses.
        /// </summary>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="condition">Condition delegate.</param>
        /// <returns>The successful result when condition returns non-null.</returns>
        public TResult Execute<TResult>(Func<TResult> condition)
        {
            TResult Func(bool b) => condition();
            try
            {
                return this.wait.Execute(Func);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
                throw; // kept for caller clarity; helper always throws
            }
        }

        /// <summary>
        /// Execute an asynchronous condition until it returns a non-null result or timeout elapses.
        /// </summary>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="condition">Async condition delegate that returns a task.</param>
        /// <returns>A task that resolves to the successful result.</returns>
        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition)
        {
            try
            {
                return await wait.ExecuteAsync(condition);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
                throw;
            }
        }

        /// <summary>
        /// Execute an asynchronous condition until it returns a non-null result or timeout elapses,
        /// supporting cancellation via <paramref name="token"/>.
        /// </summary>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="condition">Async condition delegate that returns a task.</param>
        /// <param name="token">Cancellation token to cancel the wait.</param>
        /// <returns>A task that resolves to the successful result.</returns>
        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition, CancellationToken token)
        {
            try
            {
                return await wait.ExecuteAsync(condition, token).ConfigureAwait(false);
            }
            catch (TimeoutException e)
            {
                ExceptionHelpers.ThrowConfiguredOrDefault(this.exceptionType, e);
                throw;
            }
        }
    }
}