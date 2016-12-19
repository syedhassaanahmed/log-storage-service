using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace ValidationPipeline.LogStorage.Tests
{
    public class BasicAuthenticationMiddlewareTests
    {
        private readonly TestServer _testServer;

        public BasicAuthenticationMiddlewareTests()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>(); // Borrow Startup logic we have from app under test

            _testServer = new TestServer(webHostBuilder);
        }

        [Fact]
        public async Task AuthenticationMiddleware_NoHeader_ReturnsUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticationMiddleware_HeaderValueSchemeNotBasic_ReturnsUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("SomeOtherAuth", string.Empty);

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticationMiddleware_HeaderValueParamEmpty_ReturnsUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", string.Empty);

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticationMiddleware_HeaderValueParamNotBase64_ReturnsUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", "username:password");

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticationMiddleware_HeaderValueParamMissingUsername_ReturnsUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();
            var credentialBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(":password");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialBytes));

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticationMiddleware_HeaderValueParamMissingPassword_ReturnsUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();
            var credentialBytes = Encoding.GetEncoding("iso-8859-1").GetBytes("username:");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialBytes));

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticationMiddleware_CorrectCredential_DoesNotReturnUnauthorized()
        {
            // Arrange
            var client = _testServer.CreateClient();
            var credentialBytes = Encoding.GetEncoding("iso-8859-1").GetBytes("Jon:Snow");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialBytes));

            // Act
            var response = await client.GetAsync("/api/logs/filename.zip");

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
