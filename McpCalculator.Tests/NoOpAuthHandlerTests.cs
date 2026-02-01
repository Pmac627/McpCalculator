using System.Security.Claims;
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
    /// <summary>
    /// Unit tests for the NoOpAuthHandler.
    /// Verifies that the handler always succeeds and creates Anonymous principal.
    /// </summary>
    public class NoOpAuthHandlerTests
    {
        private readonly Mock<IOptionsMonitor<ApiKeyAuthOptions>> _optionsMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<NoOpAuthHandler>> _loggerMock;
        private readonly UrlEncoder _urlEncoder;

        public NoOpAuthHandlerTests()
        {
            _optionsMock = new Mock<IOptionsMonitor<ApiKeyAuthOptions>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerMock = new Mock<ILogger<NoOpAuthHandler>>();
            _urlEncoder = UrlEncoder.Default;

            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_loggerMock.Object);
        }

        private NoOpAuthHandler CreateHandler(ApiKeyAuthOptions options, HttpContext httpContext)
        {
            _optionsMock.Setup(x => x.Get(It.IsAny<string>())).Returns(options);
            _optionsMock.Setup(x => x.CurrentValue).Returns(options);

            var handler = new NoOpAuthHandler(
                _optionsMock.Object,
                _loggerFactoryMock.Object,
                _urlEncoder);

            var scheme = new AuthenticationScheme("NoOp", "NoOp", typeof(NoOpAuthHandler));
            handler.InitializeAsync(scheme, httpContext).Wait();

            return handler;
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithNoHeaders_ReturnsSuccess()
        {
            // Arrange
            var options = new ApiKeyAuthOptions();
            var httpContext = new DefaultHttpContext();
            // No headers added - should still succeed

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithApiKeyHeader_StillReturnsSuccess()
        {
            // Arrange - NoOp handler should ignore any headers
            var options = new ApiKeyAuthOptions
            {
                HeaderName = "X-API-Key",
                ValidKeys = new List<string> { "some-key" }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-API-Key"] = "any-value-ignored";

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert - Should succeed regardless of header value
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ReturnsAnonymousPrincipalName()
        {
            // Arrange
            var options = new ApiKeyAuthOptions();
            var httpContext = new DefaultHttpContext();

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
            Assert.Equal("Anonymous", result.Principal.Identity?.Name);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_SetsAuthenticationMethodToNone()
        {
            // Arrange
            var options = new ApiKeyAuthOptions();
            var httpContext = new DefaultHttpContext();

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            var claims = result.Principal!.Claims.ToList();
            Assert.Contains(claims, c => c.Type == ClaimTypes.AuthenticationMethod && c.Value == "None");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_DoesNotIncludeApiKeyPrefixClaim()
        {
            // Arrange - Unlike ApiKeyAuthHandler, NoOpAuthHandler should not include ApiKeyPrefix claim
            var options = new ApiKeyAuthOptions();
            var httpContext = new DefaultHttpContext();

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            var claims = result.Principal!.Claims.ToList();
            Assert.DoesNotContain(claims, c => c.Type == "ApiKeyPrefix");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_UsesCorrectSchemeName()
        {
            // Arrange
            var options = new ApiKeyAuthOptions();
            var httpContext = new DefaultHttpContext();

            var handler = CreateHandler(options, httpContext);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal?.Identity);
            Assert.Equal("NoOp", result.Principal.Identity.AuthenticationType);
        }
    }
}
