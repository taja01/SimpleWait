using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.Core
{
    internal class AsyncDefaultWait<T> : IAsyncWait<T>
    {
        protected T input;
        protected IClock clock;

        protected TimeSpan timeout = DefaultSleepTimeout;
        protected TimeSpan sleepInterval = DefaultSleepTimeout;
        protected string message = string.Empty;

        private List<Type> ignoredExceptions = new List<Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultWait&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="input">The input value to pass to the evaluated conditions.</param>
        public AsyncDefaultWait(T input)
            : this(input, new SystemClock())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultWait&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="input">The input value to pass to the evaluated conditions.</param>
        /// <param name="clock">The clock to use when measuring the timeout.</param>
        public AsyncDefaultWait(T input, IClock clock)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input", "input cannot be null");
            }

            if (clock == null)
            {
                throw new ArgumentNullException("clock", "clock cannot be null");
            }

            this.input = input;
            this.clock = clock;
        }

        /// <summary>
        /// Gets or sets how long to wait for the evaluated condition to be true. The default timeout is 500 milliseconds.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        /// <summary>
        /// Gets or sets how often the condition should be evaluated. The default timeout is 500 milliseconds.
        /// </summary>
        public TimeSpan PollingInterval
        {
            get { return this.sleepInterval; }
            set { this.sleepInterval = value; }
        }

        /// <summary>
        /// Gets or sets the message to be displayed when time expires.
        /// </summary>
        public string Message
        {
            get { return this.message; }
            set { this.message = value; }
        }

        private static TimeSpan DefaultSleepTimeout
        {
            get { return TimeSpan.FromMilliseconds(500); }
        }

        /// <summary>
        /// Configures this instance to ignore specific types of exceptions while waiting for a condition.
        /// Any exceptions not whitelisted will be allowed to propagate, terminating the wait.
        /// </summary>
        /// <param name="exceptionTypes">The types of exceptions to ignore.</param>
        public void IgnoreExceptionTypes(params Type[] exceptionTypes)
        {
            if (exceptionTypes == null)
            {
                throw new ArgumentNullException("exceptionTypes", "exceptionTypes cannot be null");
            }

            foreach (Type exceptionType in exceptionTypes)
            {
                if (!typeof(Exception).IsAssignableFrom(exceptionType))
                {
                    throw new ArgumentException("All types to be ignored must derive from System.Exception", "exceptionTypes");
                }
            }

            this.ignoredExceptions.AddRange(exceptionTypes);
        }

        protected virtual void ThrowTimeoutException(string exceptionMessage, Exception lastException)
        {
            throw new TimeoutException(exceptionMessage, lastException);
        }

        protected bool IsIgnoredException(Exception exception)
        {
            return this.ignoredExceptions.Any(type => type.IsAssignableFrom(exception.GetType()));
        }

        public virtual async Task<TResult> UntilAsync<TResult>(Func<T, TResult> condition)
        {
            return await UntilAsync(condition, CancellationToken.None);
        }

        public virtual async Task<TResult> UntilAsync<TResult>(Func<T, TResult> condition, CancellationToken token)
        {
            if (condition == null)
            {
                throw new ArgumentNullException("condition", "condition cannot be null");
            }

            var resultType = typeof(TResult);


            if (resultType.BaseType.FullName != "System.Threading.Tasks.Task")
            {
                throw new ArgumentException("Can only wait on an object or boolean response, tried to use type: " + resultType.ToString(), "condition");
            }

            Exception lastException = null;
            var endTime = this.clock.LaterBy(this.timeout);
            while (true)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var conditionResult = condition(this.input);

                    if (conditionResult != null)
                    {
                        PropertyInfo[] properties = resultType.GetProperties();

                        var v = properties.First().GetValue(conditionResult);

                        if (resultType == typeof(Task<bool?>))
                        {
                            var boolResult = conditionResult as Task<bool?>;

                            var b = await boolResult;
                            if (b.HasValue && b.Value)
                            {
                                return conditionResult;
                            }
                        }
                        else
                        {
                            return conditionResult;
                        }
                    }
                }
                catch (TargetInvocationException)
                {
                    //TODO
                }
                catch (Exception ex)
                {
                    if (!this.IsIgnoredException(ex))
                    {
                        throw;
                    }

                    lastException = ex;
                }

                // Check the timeout after evaluating the function to ensure conditions
                // with a zero timeout can succeed.
                if (!this.clock.IsNowBefore(endTime))
                {
                    string timeoutMessage = string.Format(CultureInfo.InvariantCulture, "Timed out after {0} seconds", this.timeout.TotalSeconds);
                    if (!string.IsNullOrEmpty(this.message))
                    {
                        timeoutMessage += ": " + this.message;
                    }

                    this.ThrowTimeoutException(timeoutMessage, lastException);
                }

                await Task.Delay(this.sleepInterval);
            }
        }
    }
}
