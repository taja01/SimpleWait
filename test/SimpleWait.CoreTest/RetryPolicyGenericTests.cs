using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class RetryPolicyGenericTests
    {
        // Custom exception for testing wrapping behavior
        public class TestTimeoutException : Exception
        {
            public TestTimeoutException() { }
            public TestTimeoutException(string message) : base(message) { }
            public TestTimeoutException(string message, Exception inner) : base(message, inner) { }
        }

        [Test]
        public void Execute_Generic_ReturnsTypedResult_WhenConditionEventuallySatisfies()
        {
            var attempts = 0;
            var policy = RetryPolicy.For<string>()
                .Timeout(TimeSpan.FromMilliseconds(500))
                .PollingInterval(TimeSpan.FromMilliseconds(10));

            string result = policy.Execute(() =>
            {
                attempts++;
                return attempts >= 3 ? "ready" : null;
            });

            Assert.That(result, Is.EqualTo("ready"));
            Assert.That(attempts, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public async Task ExecuteAsync_Generic_ReturnsTypedResult_WhenConditionEventuallySatisfies()
        {
            var attempts = 0;
            var policy = RetryPolicy.For<string>()
                .Timeout(TimeSpan.FromMilliseconds(1000))
                .PollingInterval(TimeSpan.FromMilliseconds(10));

            var result = await policy.ExecuteAsync(async () =>
            {
                attempts++;
                await Task.Yield();
                return attempts >= 4 ? "async-ready" : null;
            }).ConfigureAwait(false);

            Assert.That(result, Is.EqualTo("async-ready"));
            Assert.That(attempts, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void Success_GenericPredicate_ReturnsTrueWhenPredicateMatches()
        {
            var value = 0;
            var policy = RetryPolicy.For<int>()
                .Timeout(TimeSpan.FromMilliseconds(500))
                .PollingInterval(TimeSpan.FromMilliseconds(10));

            bool ok = policy.Success(
                condition: () => { value++; return value; },
                success: v => v >= 5);

            Assert.That(ok, Is.True);
            Assert.That(value, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void Execute_Generic_WithConfiguredThrow_PreservesInnerException()
        {
            var policy = RetryPolicy.For<object>()
                .Timeout(TimeSpan.FromMilliseconds(50))
                .PollingInterval(TimeSpan.FromMilliseconds(10))
                .Throw<TestTimeoutException>();

            var ex = Assert.Throws<TestTimeoutException>(() =>
            {
                policy.Execute(() => (object?)null);
            });

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.InnerException, Is.TypeOf<TimeoutException>());
        }

        [Test]
        public void ExecuteAsync_Generic_HonorsCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            var policy = RetryPolicy.For<object>()
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
    }
}