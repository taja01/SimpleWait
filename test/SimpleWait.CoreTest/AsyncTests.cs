using NUnit.Framework;
using RestSharp;
using SimpleWait.Core;
using System;
using System.Threading.Tasks;

namespace SimpleWait.CoreTest
{
    [TestFixture]
    public class AsyncTests
    {
        private IRestClient client;

        [SetUp]
        public void Setup()
        {
            client = new RestClient("https://petstore.swagger.io/v2");
        }

        [Test]
        public async Task TimeoutTest()
        {
            var request = new RestRequest("pet/0", Method.Get);

            Assert.ThrowsAsync<TimeoutException>(() => AsyncWait.Initialize().Until(async () =>
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
