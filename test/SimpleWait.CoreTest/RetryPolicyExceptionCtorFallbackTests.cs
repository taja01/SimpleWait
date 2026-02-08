using NUnit.Framework;
using SimpleWait.Core;
using System;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class RetryPolicyExceptionCtorFallbackTests
    {
        public class WithInnerCtorException : Exception
        {
            public WithInnerCtorException() { }
            public WithInnerCtorException(string message) : base(message) { }
            public WithInnerCtorException(string message, Exception inner) : base(message, inner) { }
        }

        public class StringCtorException : Exception
        {
            public StringCtorException(string message) : base(message) { }
        }

        public class ParameterlessException : Exception
        {
            public ParameterlessException() { }
        }

        public class PrivateCtorException : Exception
        {
            private PrivateCtorException() { }
        }

        [Test]
        public void WithInnerCtor_PreservesInnerException()
        {
            try
            {
                RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromMilliseconds(100))
                    .Throw<WithInnerCtorException>()
                    .Execute<object>(() => null);

                Assert.Fail("Expected WithInnerCtorException was not thrown.");
            }
            catch (WithInnerCtorException ex)
            {
                Assert.That(ex.InnerException, Is.Not.Null, "Inner exception should be preserved when (string, Exception) ctor exists.");
                Assert.That(ex.InnerException, Is.TypeOf<TimeoutException>());
            }
        }

        [Test]
        public void StringCtor_UsesMessage_NoInnerException()
        {
            try
            {
                RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromMilliseconds(100))
                    .Throw<StringCtorException>()
                    .Execute<object>(() => null);

                Assert.Fail("Expected StringCtorException was not thrown.");
            }
            catch (StringCtorException ex)
            {
                Assert.That(ex.InnerException, Is.Null, "No InnerException expected when only (string) ctor exists.");
                Assert.That(ex.Message, Does.Contain("Timed out after").Or.Not.Null);
            }
        }

        [Test]
        public void ParameterlessCtor_Constructed_NoInnerException()
        {
            try
            {
                RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromMilliseconds(100))
                    .Throw<ParameterlessException>()
                    .Execute<object>(() => null);

                Assert.Fail("Expected ParameterlessException was not thrown.");
            }
            catch (ParameterlessException ex)
            {
                Assert.That(ex.InnerException, Is.Null, "No InnerException expected when only parameterless ctor exists.");
                Assert.That(ex.GetType(), Is.EqualTo(typeof(ParameterlessException)));
            }
        }

        [Test]
        public void NoPublicCtor_LeadsToInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                RetryPolicy.Initialize()
                    .Timeout(TimeSpan.FromMilliseconds(100))
                    .Throw<PrivateCtorException>()
                    .Execute<object>(() => null);
            }, "Should throw InvalidOperationException when configured exception cannot be constructed.");
        }
    }
}