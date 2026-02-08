using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    internal interface IWait<T>
    {
        /// <summary>
        /// Gets or sets how long to wait for the evaluated condition to be true.
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets or sets how often the condition should be evaluated.
        /// </summary>
        TimeSpan PollingInterval { get; set; }

        /// <summary>
        /// Gets or sets the message to be displayed when time expires.
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// Configures this instance to ignore specific types of exceptions while waiting for a condition.
        /// Any exceptions not whitelisted will be allowed to propagate, terminating the wait.
        /// </summary>
        /// <param name="exceptionTypes">The types of exceptions to ignore.</param>
        void IgnoreExceptionTypes(params Type[] exceptionTypes);

        /// <summary>
        /// Waits until a condition is true or times out.
        /// </summary>
        /// <typeparam name="TResult">The type of result to expect from the condition.</typeparam>
        /// <param name="condition">A delegate taking a TSource as its parameter, and returning a TResult.</param>
        /// <returns>If TResult is a boolean, the method returns <see langword="true"/> when the condition is true, and <see langword="false"/> otherwise.
        /// If TResult is an object, the method returns the object when the condition evaluates to a value other than <see langword="null"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when TResult is not boolean or an object type.</exception>
        TResult Execute<TResult>(Func<T, TResult> condition);

        /// <summary>
        /// Waits until a condition is true or times out.
        /// </summary>
        /// <typeparam name="TResult">The type of result to expect from the condition.</typeparam>
        /// <param name="condition">A delegate taking a TSource as its parameter, and returning a TResult.</param>
        /// <param name="token">Cancellation token to cancel the wait.</param>
        /// <returns>If TResult is a boolean, the method returns <see langword="true"/> when the condition is true, and <see langword="false"/> otherwise.
        /// If TResult is an object, the method returns the object when the condition evaluates to a value other than <see langword="null"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when TResult is not boolean or an object type.</exception>
        TResult Execute<TResult>(Func<T, TResult> condition, CancellationToken token);

        /// <summary>
        /// Waits until an async condition is true or times out.
        /// </summary>
        /// <typeparam name="TResult">The type of result to expect from the condition.</typeparam>
        /// <param name="condition">An async delegate returning TResult.</param>
        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition);

        /// <summary>
        /// Waits until an async condition is true or times out, supporting cancellation.
        /// </summary>
        /// <typeparam name="TResult">The type of result to expect from the condition.</typeparam>
        /// <param name="condition">An async delegate returning TResult.</param>
        /// <param name="token">Cancellation token to cancel the wait.</param>
        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> condition, CancellationToken token);
    }
}