using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpCalculator.Web.Api;
using McpCalculator.Web.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpCalculator.Tests
{
    /// <summary>
    /// Integration tests for the Calculator REST API endpoints.
    /// Tests the full HTTP pipeline including routing, serialization, and error handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tests use WebApplicationFactory to create a test server with authentication
    /// disabled (Type: "None") to focus on testing the API logic.
    /// </para>
    /// <para>
    /// Rate limit tests are commented out because the rate limiter is static and shared
    /// across all test instances, causing conflicts with parallel test execution.
    /// To run rate limit tests: dotnet test --max-parallelization 1
    /// </para>
    /// </remarks>
    public class CalculatorApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public CalculatorApiEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Disable authentication for testing
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Type"] = "None"
                    });
                });
            }).CreateClient();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        #region Success Cases

        [Fact]
        public async Task Add_WithValidNumbers_Returns200WithCorrectResult()
        {
            // Arrange
            var request = new CalculatorRequest(10.5, 3.2);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/add", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(13.7, result.Result, 10);
            Assert.Equal("Add", result.Operation);
        }

        [Fact]
        public async Task Subtract_WithValidNumbers_Returns200WithCorrectResult()
        {
            // Arrange
            var request = new CalculatorRequest(10.5, 3.2);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/subtract", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(7.3, result.Result, 10);
            Assert.Equal("Subtract", result.Operation);
        }

        [Fact]
        public async Task Multiply_WithValidNumbers_Returns200WithCorrectResult()
        {
            // Arrange
            var request = new CalculatorRequest(4.0, 2.5);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/multiply", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(10.0, result.Result, 10);
            Assert.Equal("Multiply", result.Operation);
        }

        [Fact]
        public async Task Divide_WithValidNumbers_Returns200WithCorrectResult()
        {
            // Arrange
            var request = new CalculatorRequest(10.0, 4.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/divide", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(2.5, result.Result, 10);
            Assert.Equal("Divide", result.Operation);
        }

        #endregion

        #region Validation Error Cases (400 Bad Request)

        // Note: NaN and Infinity tests are not included here because JSON does not support
        // these values. The validation for NaN/Infinity is covered in CalculatorToolsTests.

        [Fact]
        public async Task Add_WithValueExceedingLimit_Returns400BadRequest()
        {
            // Arrange - Value exceeds ±1e15 limit
            var request = new CalculatorRequest(2e15, 5.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/add", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<CalculatorErrorResponse>(_jsonOptions);
            Assert.NotNull(error);
            Assert.Equal("ValidationError", error.Error);
            Assert.Contains("exceeds maximum", error.Message);
        }

        [Fact]
        public async Task Divide_ByZero_Returns400BadRequest()
        {
            // Arrange
            var request = new CalculatorRequest(10.0, 0.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/divide", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<CalculatorErrorResponse>(_jsonOptions);
            Assert.NotNull(error);
            Assert.Equal("ValidationError", error.Error);
            Assert.Contains("zero", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Divide_ByNearZero_Returns400BadRequest()
        {
            // Arrange - Denominator absolute value < 1e-10
            var request = new CalculatorRequest(10.0, 1e-11);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/divide", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<CalculatorErrorResponse>(_jsonOptions);
            Assert.NotNull(error);
            Assert.Equal("ValidationError", error.Error);
        }

        #endregion

        #region Overflow Error Cases (400 Bad Request)

        [Fact]
        public async Task Multiply_ResultExceedsLimit_Returns400OverflowError()
        {
            // Arrange - Result will exceed ±1e15
            var request = new CalculatorRequest(1e14, 100.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/multiply", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<CalculatorErrorResponse>(_jsonOptions);
            Assert.NotNull(error);
            Assert.Equal("OverflowError", error.Error);
            Assert.Contains("exceeded", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Response Format Tests

        [Fact]
        public async Task SuccessResponse_HasCorrectJsonSchema()
        {
            // Arrange
            var request = new CalculatorRequest(1.0, 1.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/add", request);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            // Assert
            Assert.True(doc.RootElement.TryGetProperty("result", out var resultProp));
            Assert.True(doc.RootElement.TryGetProperty("operation", out var operationProp));
            Assert.Equal(JsonValueKind.Number, resultProp.ValueKind);
            Assert.Equal(JsonValueKind.String, operationProp.ValueKind);
        }

        [Fact]
        public async Task ErrorResponse_HasCorrectJsonSchema()
        {
            // Arrange - Use value exceeding limit to trigger error
            var request = new CalculatorRequest(2e15, 1.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/add", request);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            // Assert
            Assert.True(doc.RootElement.TryGetProperty("error", out var errorProp));
            Assert.True(doc.RootElement.TryGetProperty("message", out var messageProp));
            Assert.True(doc.RootElement.TryGetProperty("operation", out var operationProp));
            Assert.Equal(JsonValueKind.String, errorProp.ValueKind);
            Assert.Equal(JsonValueKind.String, messageProp.ValueKind);
            Assert.Equal(JsonValueKind.String, operationProp.ValueKind);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Add_WithNegativeNumbers_Returns200WithCorrectResult()
        {
            // Arrange
            var request = new CalculatorRequest(-5.0, -3.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/add", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(-8.0, result.Result, 10);
        }

        [Fact]
        public async Task Divide_WithNegativeResult_Returns200WithCorrectResult()
        {
            // Arrange
            var request = new CalculatorRequest(-10.0, 4.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/divide", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(-2.5, result.Result, 10);
        }

        [Fact]
        public async Task Multiply_WithZero_Returns200WithZeroResult()
        {
            // Arrange
            var request = new CalculatorRequest(100.0, 0.0);

            // Act
            var response = await _client.PostAsJsonAsync("/api/calculator/multiply", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(0.0, result.Result, 10);
        }

        #endregion

        #region Rate Limit Tests (Commented Out)
        // Note: These tests are commented out because xUnit runs tests in parallel
        // and the rate limiter is shared (static) across all test instances.
        // To run these tests: dotnet test --max-parallelization 1

        /*
        [Fact]
        public async Task Add_ExceedingRateLimit_Returns429TooManyRequests()
        {
            // Arrange - Rate limit is 100 requests per minute
            var request = new CalculatorRequest(1.0, 1.0);

            // Use up the rate limit
            for (int i = 0; i < 100; i++)
            {
                await _client.PostAsJsonAsync("/api/calculator/add", request);
            }

            // Act - This request should exceed the limit
            var response = await _client.PostAsJsonAsync("/api/calculator/add", request);

            // Assert
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task DifferentOperations_HaveSeparateRateLimits()
        {
            // Arrange - Each operation has its own rate limit
            var request = new CalculatorRequest(1.0, 1.0);

            // Use up Add's rate limit
            for (int i = 0; i < 100; i++)
            {
                await _client.PostAsJsonAsync("/api/calculator/add", request);
            }

            // Act - Multiply should still work
            var response = await _client.PostAsJsonAsync("/api/calculator/multiply", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        */
        #endregion

        #region Unit Tests - ExecuteOperation Exception Handling

        /// <summary>
        /// Unit tests that directly call ExecuteOperation to test exception handling paths
        /// that are difficult to trigger through HTTP integration tests.
        /// </summary>

        [Fact]
        public void ExecuteOperation_WithRateLimitException_Returns429TooManyRequests()
        {
            // Arrange
            var request = new CalculatorRequest(1.0, 1.0);

            // Act - Simulate rate limit exception
            var result = CalculatorApiEndpoints.ExecuteOperation(
                request,
                "Add",
                () => throw new InvalidOperationException("Rate limit exceeded for operation"));

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeHttpResult>(result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, statusCodeResult.StatusCode);
        }

        [Fact]
        public void ExecuteOperation_WithGenericException_Returns500InternalServerError()
        {
            // Arrange
            var request = new CalculatorRequest(1.0, 1.0);
            var unexpectedMessage = "Something unexpected happened";

            // Act - Simulate unexpected exception
            var result = CalculatorApiEndpoints.ExecuteOperation(
                request,
                "Add",
                () => throw new NotImplementedException(unexpectedMessage));

            // Assert
            var problemResult = Assert.IsType<ProblemHttpResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        }

        #endregion
    }
}
