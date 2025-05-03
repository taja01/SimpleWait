using Moq;
using NUnit.Framework;
using SimpleWait.Core;
using System;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    internal class AsyncMockTests
    {
        [Test]
        public async Task NullableIntegerTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetMagicNumberAsync())
                .ReturnsAsync((int?)null)
                .ReturnsAsync(2)
                .ReturnsAsync(4);

            var result = await AsyncRetryPolicy.Initialize()
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.GetMagicNumberAsync();

                    if (r.HasValue && r.Value == 4)
                    {
                        return r.Value;
                    }

                    return (int?)null;
                });


            mockService.Verify(m => m.GetMagicNumberAsync(), Times.Exactly(3));

            Assert.That(result, Is.EqualTo(4));

        }

        [Test]
        public async Task IntegerTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetFixMagicNumberAsync())
                .ReturnsAsync(2)
                .ReturnsAsync(4);

            var result = await AsyncRetryPolicy.Initialize()
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.GetFixMagicNumberAsync();

                    if (r == 4)
                    {
                        return r;
                    }

                    return (int?)null;
                });


            mockService.Verify(m => m.GetFixMagicNumberAsync(), Times.Exactly(2));

            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public async Task NullableStringTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetMagicString())
                .ReturnsAsync((string)null)
                .ReturnsAsync("42");

            var result = await AsyncRetryPolicy.Initialize()
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.GetMagicString();

                    if (r != null)
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetMagicString(), Times.Exactly(2));

            Assert.That(result, Is.EqualTo("42"));
        }

        [Test]
        public async Task StringTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetFixMagicString())
                .ReturnsAsync((string)null)
                .ReturnsAsync("")
                .ReturnsAsync("42");

            var result = await AsyncRetryPolicy.Initialize().Timeout(System.TimeSpan.FromHours(1))
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.GetFixMagicString();

                    if (r != null && r == "42")
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetFixMagicString(), Times.Exactly(3));

            Assert.That(result, Is.EqualTo("42"));
        }

        [Test]
        public async Task CustomObjectTest()
        {
            var s = new Response();
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetResponse())
                .ReturnsAsync((Response)null)
                .ReturnsAsync(s);

            var result = await AsyncRetryPolicy.Initialize()
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.GetResponse();

                    if (r != null)
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetResponse(), Times.Exactly(2));

            Assert.That(result, Is.EqualTo(s));
        }

        [Test]
        public async Task CustomObjectWithAdditionalTest()
        {
            var response1 = default(Response);
            var response2 = new Response(3);
            var response3 = new Response(10);

            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetResponse())
                .ReturnsAsync((Response)null)
                .ReturnsAsync(response1)
                .ReturnsAsync(response2)
                .ReturnsAsync(response3);

            var result = await AsyncRetryPolicy.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.GetResponse();

                    if (r != null && r.Size == 10)
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetResponse(), Times.Exactly(4));

            Assert.That(result, Is.EqualTo(response3));
        }

        [Test]
        public async Task BooleanTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.IsFixAliveAsync())
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            var result = await AsyncRetryPolicy.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.IsFixAliveAsync();

                    if (r)
                    {
                        return r;
                    }
                    else
                    {
                        return false;
                    }
                });


            mockService.Verify(m => m.IsFixAliveAsync(), Times.Exactly(2));

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task NullableBooleanTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.IsAliveAsync())
                .ReturnsAsync((bool?)null)
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            var result = await AsyncRetryPolicy.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.IsAliveAsync();

                    if (r.HasValue)
                    {
                        return r;
                    }
                    else
                    {
                        return false;
                    }
                });


            mockService.Verify(m => m.IsAliveAsync(), Times.Exactly(3));

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ExceptionTest()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.IsAliveAsync())
                .ThrowsAsync(new System.Net.WebException())
                .ReturnsAsync(true);

            var result = await AsyncRetryPolicy.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .IgnoreExceptionTypes(typeof(System.Net.WebException))
                .ExecuteAsync(async () =>
                {
                    var r = await mockService.Object.IsAliveAsync();

                    if (r.HasValue)
                    {
                        return r;
                    }
                    else { return false; }
                });


            mockService.Verify(m => m.IsAliveAsync(), Times.Exactly(2));

            Assert.That(result, Is.True);
        }
    }

    public class MyService : IMyService
    {
        public Task<int> GetFixMagicNumberAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFixMagicString()
        {
            throw new NotImplementedException();
        }

        public Task<int?> GetMagicNumberAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMagicString()
        {
            throw new NotImplementedException();
        }

        public Task<Response> GetResponse()
        {
            throw new NotImplementedException();
        }

        public Task<bool?> IsAliveAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsFixAliveAsync()
        {
            throw new NotImplementedException();
        }
    }

    public interface IMyService
    {
        Task<int?> GetMagicNumberAsync();
        Task<string> GetMagicString();
        Task<bool?> IsAliveAsync();
        Task<Response> GetResponse();

        Task<int> GetFixMagicNumberAsync();
        Task<string> GetFixMagicString();
        Task<bool> IsFixAliveAsync();
    }

    public class Response
    {
        public int Size { get; set; }

        public Response(int size)
        {
            Size = size;
        }

        public Response() { }
    }

}