using NUnit.Framework;
using RestSharp;
using SimpleWait.Core;
using System;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class AsyncTests
    {
        private RestClient client;

        [SetUp]
        public void Setup()
        {
            client = new RestClient("https://petstore.swagger.io/v2");
        }

        [Test]
        public void TimeoutTest()
        {
            var request = new RestRequest("pet/0", Method.Get);

            Assert.ThrowsAsync<TimeoutException>(async () => await AsyncWait.Initialize().UntilAsync(async () =>
            {
                var response = await client.ExecuteAsync(request);

                if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    return response;
                }
                else return null;
            }));
        }
    }
}
