using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Diagnostics;

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
    }

    class WorkingClass
    {

    }
}