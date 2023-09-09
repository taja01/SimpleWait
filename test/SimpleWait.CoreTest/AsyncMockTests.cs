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
        public async Task Test()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetMagicNumberAsync())
                .ReturnsAsync((int?)null)
                .ReturnsAsync(2)
                .ReturnsAsync(4);

            var result = await AsyncWait.Initialize()
                .Until(async () =>
                {
                    var r = await mockService.Object.GetMagicNumberAsync();

                    if (r.HasValue && r.Value == 4)
                    {
                        return r.Value;
                    }

                    return (int?)null;
                });


            mockService.Verify(m => m.GetMagicNumberAsync(), Times.Exactly(3));

            Assert.AreEqual(4, result.Result);

        }

        [Test]
        public async Task Test1()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetMagicNumberAsync())
                .ReturnsAsync(2)
                .ReturnsAsync(4);

            var result = await AsyncWait.Initialize()
                .Until(async () =>
                {
                    var r = await mockService.Object.GetMagicNumberAsync();

                    if (r.HasValue && r.Value == 4)
                    {
                        return r.Value;
                    }

                    return (int?)null;
                });


            mockService.Verify(m => m.GetMagicNumberAsync(), Times.Exactly(2));

            Assert.AreEqual(4, result.Result);

        }

        [Test]
        public async Task Test2()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetMagicString())
                .ReturnsAsync((string?)null)
                .ReturnsAsync("42");

            var result = await AsyncWait.Initialize()
                .Until(async () =>
                {
                    var r = await mockService.Object.GetMagicString();

                    if (r != null)
                    {
                        return r;
                    }

                    return (string?)null;
                });


            mockService.Verify(m => m.GetMagicString(), Times.Exactly(2));

            Assert.AreEqual("42", result.Result);
        }

        [Test]
        public async Task Test3()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetMagicString())
                .ReturnsAsync((string)null)
                .ReturnsAsync("")
                .ReturnsAsync("42");

            var result = await AsyncWait.Initialize()
                .Until(async () =>
                {
                    var r = await mockService.Object.GetMagicString();

                    if (r != null && r == "42")
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetMagicString(), Times.Exactly(3));

            Assert.AreEqual("42", result.Result);
        }

        [Test]
        public async Task Test4()
        {
            var s = new Response();
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetResponse())
                .ReturnsAsync((Response)null)
                .ReturnsAsync(s);

            var result = await AsyncWait.Initialize()
                .Until(async () =>
                {
                    var r = await mockService.Object.GetResponse();

                    if (r != null)
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetResponse(), Times.Exactly(2));

            Assert.AreEqual(s, result.Result);
        }

        [Test]
        public async Task Test5()
        {
            var s = new Response();
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetResponse())
                .ReturnsAsync(default(Response))
                .ReturnsAsync(s);

            var result = await AsyncWait.Initialize()
                .Until(async () =>
                {
                    var r = await mockService.Object.GetResponse();

                    if (r != null)
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetResponse(), Times.Exactly(2));

            Assert.AreEqual(s, result.Result);
        }

        [Test]
        public async Task Test6()
        {
            var response1 = new Response();
            var response2 = new Response(3);
            var response3 = new Response(10);

            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.GetResponse())
                .ReturnsAsync((Response)null)
                .ReturnsAsync(response1)
                .ReturnsAsync(response2)
                .ReturnsAsync(response3);

            var result = await AsyncWait.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .Until(async () =>
                {
                    var r = await mockService.Object.GetResponse();

                    if (r != null && r.Size == 10)
                    {
                        return r;
                    }

                    return null;
                });


            mockService.Verify(m => m.GetResponse(), Times.Exactly(4));

            Assert.AreEqual(response3, result.Result);
        }

        [Test]
        public async Task Test7()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.IsAliveAsync())
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            var result = await AsyncWait.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .Until(async () =>
                {
                    var r = await mockService.Object.IsAliveAsync();

                    if (r.HasValue)
                    {
                        return r;
                    }
                    else { return false; }
                });


            mockService.Verify(m => m.IsAliveAsync(), Times.Exactly(2));

            Assert.AreEqual(true, result.Result);
        }

        [Test]
        public async Task Test8_WithException()
        {
            var mockService = new Mock<IMyService>();
            mockService
                .SetupSequence(m => m.IsAliveAsync())
                .ThrowsAsync(new System.Net.WebException())
                .ReturnsAsync(true);

            var result = await AsyncWait.Initialize()
                .Timeout(TimeSpan.FromHours(1))
                .IgnoreExceptionTypes(typeof(System.Net.WebException))
                .Until(async () =>
                {
                    var r = await mockService.Object.IsAliveAsync();

                    if (r.HasValue)
                    {
                        return r;
                    }
                    else { return false; }
                });


            mockService.Verify(m => m.IsAliveAsync(), Times.Exactly(2));

            Assert.AreEqual(true, result.Result);
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

        public async Task<int?> GetMagicNumberAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<string?> GetMagicString()
        {
            throw new NotImplementedException();
        }

        public async Task<Response?> GetResponse()
        {
            throw new NotImplementedException();
        }

        public async Task<bool?> IsAliveAsync()
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
        Task<string?> GetMagicString();
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
