using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class CoreTests
    {
        [Test]
        public void DefaultTimeout_ThrowsTimeoutException()
        {
            Assert.Throws<TimeoutException>(() =>
                RetryPolicy.Initialize()
                    .Message("Default timeout test")
                    .Execute(() => false));
        }

        [Test]
        public void TimeoutTest_IsStable_OnTimeout()
        {
            // Make timing assertions tolerant to avoid flaky failures.
            var sw = new Stopwatch();
            sw.Start();

            Assert.Throws<TimeoutException>(() =>
                RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromSeconds(1))
                    .Message("Timeout test")
                    .Execute(() => false));

            sw.Stop();

            // Ensure we waited at least the configured timeout (allow small scheduler jitter).
            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(900), "Elapsed should be at least ~1s (allowing some jitter).");
        }

        [Test]
        public void ConfiguredException_IsThrown_OnTimeout()
        {
            Assert.That(() => RetryPolicy.Initialize()
                                              .Timeout(TimeSpan.FromSeconds(1))
                                              .Throw<InvalidOperationException>()
                                              .Execute(() => NotWorkingClass.Work()),
                        Throws.Exception.TypeOf<InvalidOperationException>().With.Message.EqualTo("Timed out after 1 seconds"));
        }

        [Test]
        public void Success_ReturnsFalse_OnTimeout()
        {
            var result = RetryPolicy.Initialize()
                                   .Message("Success test")
                                   .Success(() => false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Success_ReturnsTrue_WhenConditionBecomesTrue()
        {
            var index = 0;
            var arr = new[] { 1, 3, 5, 7, 9, 10 };

            var result = RetryPolicy.Initialize()
                .Message("Success test")
                .Success(() =>
                {
                    if (arr[index] % 2 == 0)
                    {
                        return true;
                    }

                    index++;
                    return false;
                });

            Assert.That(result, Is.True);
            Assert.That(index, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void IgnoreExceptionTypes_AllowsRetry_OnIgnoredExceptions()
        {
            var attempts = 0;
            var policy = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(1))
                .IgnoreExceptionTypes(typeof(DivideByZeroException))
                .PollingInterval(TimeSpan.FromMilliseconds(10));

            Assert.Throws<TimeoutException>(() =>
            {
                policy.Execute(() =>
                {
                    attempts++;
                    // first two attempts throw ignored exception, subsequent attempts still false
                    if (attempts <= 2)
                    {
                        throw new DivideByZeroException();
                    }

                    return false;
                });
            });

            Assert.That(attempts, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void PollingInterval_AffectsAttempts_ButIsTolerant()
        {
            var counter = 0;
            var policy = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromMilliseconds(650))
                .PollingInterval(TimeSpan.FromMilliseconds(200));

            Assert.Throws<TimeoutException>(() =>
            {
                policy.Execute(() =>
                {
                    counter++;
                    return false;
                });
            });

            // Expect multiple invocations; exact count can vary, assert lower bound
            Assert.That(counter, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Execute_Typed_ReturnsObjectInstance()
        {
            var result = RetryPolicy.Initialize()
                .Execute(() => new WorkingClass());

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<WorkingClass>());
        }

        [Test]
        public void Execute_Message_IsAppendedToTimeout()
        {
            Assert.That(() => RetryPolicy.Initialize()
                                         .Timeout(TimeSpan.FromSeconds(1))
                                         .Throw<InvalidOperationException>()
                                         .Message("My message")
                                         .Execute(() => NotWorkingClass.Work()),
                        Throws.Exception.TypeOf<InvalidOperationException>().With.Message.EqualTo("Timed out after 1 seconds: My message"));
        }

        [Test]
        public async Task ExecuteAsync_ThrowsConfiguredException_OnTimeout()
        {
            await Task.Yield(); // ensure async context
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromSeconds(1))
                    .Throw<InvalidOperationException>()
                    .ExecuteAsync(async () =>
                    {
                        return await NotWorkingClass.WorkAsync();
                    }));
        }

        [Test]
        public async Task ExecuteAsync_HonorsCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            var policy = RetryPolicy.Initialize()
                                    .Timeout(TimeSpan.FromSeconds(5))
                                    .PollingInterval(TimeSpan.FromMilliseconds(20));

            // Cancel after a short delay while the wait loop is running.
            _ = Task.Run(async () =>
            {
                await Task.Delay(50).ConfigureAwait(false);
                cts.Cancel();
            });

            // Awaiting a canceled task throws TaskCanceledException (subclass of OperationCanceledException).
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    await Task.Yield();
                    return (object?)null;
                }, cts.Token).ConfigureAwait(false);
            });
        }

        // Helper classes used by tests
        class WorkingClass { }

        class NotWorkingClass
        {
            public static object Work() => null;

            public static async Task<object> WorkAsync()
            {
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                return null;
            }
        }
    }
}