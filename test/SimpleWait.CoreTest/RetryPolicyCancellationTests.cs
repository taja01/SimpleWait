using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class RetryPolicyCancellationTests
    {
        [Test]
        public void ExecuteAsync_ImmediateCancellation_ThrowsOperationCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel(); // cancel before starting

            var policy = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(5))
                .PollingInterval(TimeSpan.FromMilliseconds(100));

            // Condition returns quickly (null) so DefaultWait will check the token between iterations.
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await policy.ExecuteAsync<object>(async () =>
                {
                    await Task.Yield();
                    return null;
                }, cts.Token);
            });
        }

        [Test]
        public async Task ExecuteAsync_DelayedCancellation_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();

            var policy = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(10))
                .PollingInterval(TimeSpan.FromMilliseconds(100));

            // Cancel after a short delay while the wait loop is running.
            _ = Task.Run(async () =>
            {
                await Task.Delay(200).ConfigureAwait(false);
                cts.Cancel();
            });

            try
            {
                await policy.ExecuteAsync<object>(async () =>
                {
                    // quick-returning condition so the loop continues and observes cancellation between iterations
                    await Task.Yield();
                    return null;
                }, cts.Token);

                Assert.Fail("Expected OperationCanceledException was not thrown.");
            }
            catch (OperationCanceledException)
            {
                Assert.Pass();
            }
        }
    }
}