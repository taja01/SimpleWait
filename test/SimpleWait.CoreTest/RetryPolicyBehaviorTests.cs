using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class RetryPolicyBehaviorTests
    {
        public class TestTimeoutException : Exception
        {
            public TestTimeoutException() { }
            public TestTimeoutException(string message) : base(message) { }
            public TestTimeoutException(string message, Exception inner) : base(message, inner) { }
        }

        [Test]
        public void Success_WithCustomThrow_ReturnsFalse()
        {
            var result = RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(1))
                .Throw<TestTimeoutException>()
                .Success(() => false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Execute_CustomException_PreservesInnerException()
        {
            try
            {
                RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromSeconds(1))
                    .Throw<TestTimeoutException>()
                    .Execute<object>(() => null);

                Assert.Fail("Expected TestTimeoutException was not thrown.");
            }
            catch (TestTimeoutException ex)
            {
                Assert.That(ex.InnerException, Is.Not.Null, "Fixed behavior: inner exception should be preserved.");
                Assert.That(ex.InnerException, Is.TypeOf<TimeoutException>());
            }
        }

        [Test]
        public async Task ExecuteAsync_CustomException_PreservesInnerException()
        {
            try
            {
                await RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromSeconds(1))
                    .Throw<TestTimeoutException>()
                    .ExecuteAsync<object>(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                        return null;
                    });

                Assert.Fail("Expected TestTimeoutException was not thrown.");
            }
            catch (TestTimeoutException ex)
            {
                Assert.That(ex.InnerException, Is.Not.Null, "Fixed behavior (async): inner exception should be preserved.");
                Assert.That(ex.InnerException, Is.TypeOf<TimeoutException>());
            }
        }
    }
}
