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
            Assert.Throws<TimeoutException>(() => Wait.Initialize().Message("Default timeout test").Until(() => false));
        }

        [Test]
        public void TimeoutTest()
        {
            var sw = new Stopwatch();
            sw.Start();
            Assert.Throws<TimeoutException>(() => Wait.Initialize().Timeout(TimeSpan.FromSeconds(1)).Message("Timeout test").Until(() => false));
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds > 1000 && sw.ElapsedMilliseconds < 1300);
        }

        [Test]
        public void ExceptionThrowTest()
        {
            Assert.Throws<DivideByZeroException>(() => Wait.Initialize().Timeout(TimeSpan.FromMilliseconds(1)).Throw<DivideByZeroException>().Message("Exception throw").Until(() => false));
        }

        [Test]
        public void TestIgnoreExceptionTest()
        {
            var zero = 0;
            var one = 1;

            Assert.Throws<TimeoutException>(() =>
            {
                Wait.Initialize()
                   .Message("Ignore Exception test")
                   .Timeout(TimeSpan.FromSeconds(1))
                   .IgnoreExceptionTypes(typeof(DivideByZeroException))
                   .Until(() => one / zero == 0);
            });
        }

        [Test]
        public void SuccessTimeoutTest()
        {
            var m_array = new[] { 1, 3, 5, 7, 9, 10 };

            var result = Wait.Initialize()
                   .Message("Success test")
                   .Success(() =>
                   {
                       return false;
                   });

            Assert.IsFalse(result);
        }

        [Test]
        public void SuccessTest()
        {
            var m_array = new[] { 1, 3, 5, 7, 9, 10 };
            var index = 0;
            var result = Wait.Initialize()
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

            Assert.IsTrue(result);
        }

        [Test]
        public void PollingTest()
        {
            var counter = -1;

            Wait.Initialize()
                .PollingInterval(TimeSpan.FromSeconds(3))
                .Success(() =>
                {
                    counter++;
                    return false;
                });

            Assert.AreEqual(2, counter);
        }

        [Test]
        public void TypedUntilTest()
        {
            var r = new WorkingClass();

            var result = Wait.Initialize()
                .Until(() =>
                {
                    return new WorkingClass();
                });

            Assert.IsNotNull(result);
            Assert.AreEqual(typeof(WorkingClass), result.GetType());
        }
    }

    class WorkingClass
    {

    }
}