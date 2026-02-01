using System.Text.Encodings.Web;
using McpCalculator.Web.Authentication;
using McpCalculator.Web.Authentication.Configuration.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace McpCalculator.Tests
{
    public class ApiKeyAuthHandlerTests
    {
        private readonly Mock<IOptionsMonitor<ApiKeyAuthOptions>> _optionsMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<ApiKeyAuthHandler>> _loggerMock;
        private readonly UrlEncoder _urlEncoder;

        public ApiKeyAuthHandlerTests()
        {
            _optionsMock = new Mock<IOptionsMonitor<ApiKeyAuthOptions>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerMock = new Mock<ILogger<ApiKeyAuthHandler>>();
            _urlEncoder = UrlEncoder.Default;

            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_loggerMock.Object);
        }

        private ApiKeyAuthHandler CreateHandler(ApiKeyAuthOptions options, HttpContext httpContext)
        {
            _optionsMock.Setup(x => x.Get(It.IsAny<string>())).Returns(options);
            _optionsMock.Setup(x => x.CurrentValue).Returns(options);

            var handler = new ApiKeyAuthHandler(
                _optionsMock.Object,
                _loggerFactoryMock.Object,
                _urlEncoder);

            var scheme = new AuthenticationScheme("ApiKey", "ApiKey", typeof(ApiKeyAuthHandler));
            handler.InitializeAsync(scheme, httpContext).Wait();

            return handler;
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithMissingHeader_ReturnsFailure()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key" }
            };

            var httpContext = new DefaultHttpContext();
            // No header added

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Missing X-API-Key header", result.Failure?.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithEmptyApiKey_ReturnsFailure()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("API key is empty", result.Failure?.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithWhitespaceApiKey_ReturnsFailure()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "   ";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("API key is empty", result.Failure?.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithInvalidApiKey_ReturnsFailure()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key-1", "valid-key-2" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "invalid-key";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Invalid API key", result.Failure?.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithShortInvalidApiKey_ReturnsFailure()
        {
            // Arrange - Tests the "****" branch in the logging statement (key.Length <= 4)
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key-1" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "bad"; // 3 chars, triggers "****" branch

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Invalid API key", result.Failure?.Message);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithValidApiKey_ReturnsSuccess()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key-1", "valid-key-2" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "valid-key-1";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
            Assert.Equal("ApiKeyClient", result.Principal.Identity?.Name);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithSecondValidKey_ReturnsSuccess()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key-1", "valid-key-2" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "valid-key-2";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithCustomHeaderName_UsesCustomHeader()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-Custom-Key",
                ValidKeys = new List<string> { "valid-key" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Custom-Key"] = "valid-key";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithValidKey_SetsCorrectClaims()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key-12345" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "valid-key-12345";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            var claims = result.Principal!.Claims.ToList();
            Assert.Contains(claims, c => c.Type == System.Security.Claims.ClaimTypes.Name && c.Value == "ApiKeyClient");
            Assert.Contains(claims, c => c.Type == System.Security.Claims.ClaimTypes.AuthenticationMethod && c.Value == "ApiKey");
            Assert.Contains(claims, c => c.Type == "ApiKeyPrefix" && c.Value == "vali"); // First 4 chars
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithShortKey_MasksKeyPrefix()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "abc" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "abc";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            var apiKeyPrefixClaim = result.Principal!.Claims.FirstOrDefault(c => c.Type == "ApiKeyPrefix");
            Assert.NotNull(apiKeyPrefixClaim);
            Assert.Equal("****", apiKeyPrefixClaim.Value); // Masked because < 4 chars
        }

        [Fact]
        public async Task HandleChallengeAsync_Returns401WithWwwAuthenticateHeader()
        {
            // Arrange
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "valid-key" }
            };

            var httpContext = new DefaultHttpContext();
            var handler = CreateHandler(options, httpContext);

            // Act
            await handler.ChallengeAsync(new AuthenticationProperties());

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
            Assert.True(httpContext.Response.Headers.ContainsKey("WWW-Authenticate"));
            Assert.Contains("ApiKey", httpContext.Response.Headers["WWW-Authenticate"].ToString());
            Assert.Contains("X-API-Key", httpContext.Response.Headers["WWW-Authenticate"].ToString());
        }

        [Fact]
        public void ApiKeyAuthOptions_DefaultHeaderName_IsXApiKey()
        {
            // Arrange & Act
            var options = new ApiKeyAuthOptions();

            // Assert
            Assert.Equal("X-API-Key", options.HeaderName);
        }

        [Fact]
        public void ApiKeyAuthOptions_DefaultValidKeys_IsEmptyList()
        {
            // Arrange & Act
            var options = new ApiKeyAuthOptions();

            // Assert
            Assert.NotNull(options.ValidKeys);
            Assert.Empty(options.ValidKeys);
        }
    }
}