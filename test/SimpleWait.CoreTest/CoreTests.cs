using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    public class Tests
    {
        [Test]
        public void DefaultTimeoutTest()
        {
            Assert.Throws<TimeoutException>(() => RetryPolicy.Initialize().Message("Default timeout test").Execute(() => false));
        }

        [Test]
        public void TimeoutTest()
        {
            var sw = new Stopwatch();
            sw.Start();
            Assert.Throws<TimeoutException>(() => RetryPolicy.Initialize().Timeout(TimeSpan.FromSeconds(1)).Message("Timeout test").Execute(() => false));
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds > 1000 && sw.ElapsedMilliseconds < 1300);
        }

        [Test]
        public void ExceptionThrowTest()
        {
            Assert.Throws<DivideByZeroException>(() => RetryPolicy.Initialize().Timeout(TimeSpan.FromMilliseconds(1)).Throw<DivideByZeroException>().Message("Exception throw").Execute(() => false));
        }

        [Test]
        public void TestIgnoreExceptionTest()
        {
            var zero = 0;
            var one = 1;

            Assert.Throws<TimeoutException>(() =>
            {
                RetryPolicy.Initialize()
                   .Message("Ignore Exception test")
                   .Timeout(TimeSpan.FromSeconds(1))
                   .IgnoreExceptionTypes(typeof(DivideByZeroException))
                   .Execute(() => one / zero == 0);
            });
        }

        [Test]
        public void SuccessTimeoutTest()
        {
            var m_array = new[] { 1, 3, 5, 7, 9, 10 };

            var result = RetryPolicy.Initialize()
                   .Message("Success test")
                   .Success(() =>
                   {
                       return false;
                   });

            Assert.That(result, Is.False);
        }

        [Test]
        public void SuccessTest()
        {
            var m_array = new[] { 1, 3, 5, 7, 9, 10 };
            var index = 0;
            var result = RetryPolicy.Initialize()
                   .Message("Success test")
                   .Success(() =>
                   {
                       if (m_array[index] % 2 == 0)
                       {
                           return true;
                       }
                       else
                       {
                           index++;
                           return false;
                       }
                   });

            Assert.That(result, Is.True);
        }

        [Test]
        public void PollingTest()
        {
            var counter = -1;

            RetryPolicy.Initialize()
                .PollingInterval(TimeSpan.FromSeconds(3))
                .Success(() =>
                {
                    counter++;
                    return false;
                });

            Assert.That(counter, Is.EqualTo(2));
        }

        [Test]
        public void TypedUntilTest()
        {
            var r = new WorkingClass();

            var result = RetryPolicy.Initialize()
                .Execute(() =>
                {
                    return new WorkingClass();
                });

            Assert.That(result, Is.Not.Null.And.TypeOf(typeof(WorkingClass)));

            //Assert.IsNotNull(result);
            //Assert.AreEqual(typeof(WorkingClass), result.GetType());
        }

        [Test]
        public void TimeOutWithOwnExceptionTest()
        {
            Assert.That(() => RetryPolicy.Initialize()
                                              .Timeout(TimeSpan.FromSeconds(1))
                                              .Throw<InvalidOperationException>()
                                              .Execute(() =>
                                              {
                                                  return NotWorkingClass.Work();
                                              }),
              Throws.Exception.TypeOf<InvalidOperationException>().With.Message.EqualTo("Timed out after 1 seconds"));
        }

        [Test]
        public void AsyncTimeOutTest()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await RetryPolicy.Initialize()
                .Timeout(TimeSpan.FromSeconds(1))
                .Throw<InvalidOperationException>()
                .ExecuteAsync(async () =>
                {
                    return await NotWorkingClass.WorkAsync();
                }));
        }

        [Test]
        public void MessageTest()
        {
            Assert.That(() => RetryPolicy.Initialize()
                                             .Timeout(TimeSpan.FromSeconds(1))
                                             .Throw<InvalidOperationException>()
                                             .Message("My message")
                                             .Execute(() =>
                                             {
                                                 return NotWorkingClass.Work();
                                             }),
             Throws.Exception.TypeOf<InvalidOperationException>().With.Message.EqualTo("Timed out after 1 seconds: My message"));
        }
    }

    class WorkingClass
    {

    }

    class NotWorkingClass
    {
        public static object Work()
        {
            return null;
        }

        public static async Task<object> WorkAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return null;
        }
    }
}