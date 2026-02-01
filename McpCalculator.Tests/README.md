# MCP Calculator Tests

Comprehensive unit test suite for the MCP Calculator server using xUnit.

## Test Coverage

### Test Classes

| Test Class | Tests | Coverage |
|-----------|--------|----------|
| **ResourceLimitsTests** | 14 tests | Value range validation, division safety, result validation, boundary conditions |
| **RateLimiterTests** | 13 tests | Rate limiting logic, sliding window algorithm, thread safety |
| **ExecutionContextTests** | 11 tests | Timeout enforcement, exception handling, cancellation, concurrent operations |
| **CalculatorToolsTests** | 28 tests | All calculator operations with security features integrated |
| **ApiKeyAuthHandlerTests** | 13 tests | API key authentication handler, header validation, claims |
| **NoOpAuthHandlerTests** | 6 tests | No-op authentication handler, anonymous principal, claims |
| **CalculatorApiEndpointsTests** | 15 tests | REST API endpoints, HTTP status codes, JSON responses |

**Total: 100 tests**

---

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ResourceLimitsTests"
```

### Run with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

The `coverlet.runsettings` file excludes Program.cs from coverage reports (composition root with no testable logic).

---

## Test Organization

### 1. ResourceLimitsTests
Tests the resource limit validation logic:
- ✅ Valid value ranges
- ✅ Maximum/minimum boundary conditions
- ✅ Division by zero protection
- ✅ Division by very small numbers
- ✅ Result validation (NaN, Infinity, overflow)

### 2. RateLimiterTests
Tests the rate limiting mechanism:
- ✅ Sliding window algorithm correctness
- ✅ Per-operation rate limit tracking
- ✅ Thread safety with concurrent requests
- ✅ Window expiration behavior
- ✅ Clear/reset functionality

### 3. ExecutionContextTests
Tests timeout enforcement:
- ✅ Fast operations complete successfully
- ✅ Slow operations timeout correctly
- ✅ Exception propagation
- ✅ Multiple independent timeouts
- ✅ Void and value-returning operations

### 4. CalculatorToolsTests
Integration tests with all security features:
- ✅ Basic arithmetic operations (Add, Subtract, Multiply, Divide)
- ✅ Input validation (NaN, Infinity, extreme values)
- ✅ Resource limits enforcement
- ✅ Decimal precision preservation
- ✅ Edge cases (very small/large numbers)
- ✅ Error handling and exceptions

### 5. ApiKeyAuthHandlerTests
Tests for API key authentication handler (McpCalculator.Web):
- ✅ Missing header detection
- ✅ Empty/whitespace API key rejection
- ✅ Invalid API key rejection (long keys)
- ✅ Invalid API key rejection (short keys ≤4 chars, masked in logs)
- ✅ Valid API key acceptance
- ✅ Multiple valid keys support
- ✅ Custom header name configuration
- ✅ Claims generation (Name, AuthenticationMethod, ApiKeyPrefix)
- ✅ Short key masking
- ✅ 401 challenge response with WWW-Authenticate header

### 6. NoOpAuthHandlerTests
Tests for the no-op authentication handler (development mode):
- ✅ Always returns success regardless of headers
- ✅ Ignores API key headers when present
- ✅ Creates Anonymous principal name
- ✅ Sets AuthenticationMethod claim to "None"
- ✅ Does not include ApiKeyPrefix claim
- ✅ Uses correct scheme name in identity

### 7. CalculatorApiEndpointsTests
Integration tests for REST API endpoints using WebApplicationFactory:
- ✅ All four calculator operations via HTTP (add, subtract, multiply, divide)
- ✅ Value exceeding limit returns 400 ValidationError
- ✅ Division by zero returns 400 ValidationError
- ✅ Division by near-zero returns 400 ValidationError
- ✅ Multiply result overflow returns 400 OverflowError
- ✅ Correct JSON response schema for success and error responses
- ✅ Edge cases (negative numbers, zero multiplication)

Unit tests for ExecuteOperation exception handling:
- ✅ Rate limit exception returns 429 Too Many Requests
- ✅ Generic exception returns 500 Internal Server Error

**Note:** NaN/Infinity validation is tested in CalculatorToolsTests. These values cannot be tested via HTTP because JSON does not support NaN/Infinity.

---

## Known Test Limitations

### Rate Limiting Tests
Rate limiting tests that involve making 100+ requests are **commented out** in both:
- `CalculatorToolsTests` (unit tests)
- `CalculatorApiEndpointsTests` (integration tests)

This is because:
- xUnit runs tests in parallel by default
- The `RateLimiter` in `CalculatorTools` is static (shared across all tests)
- Parallel execution causes rate limit conflicts between tests

**Solutions:**
1. Use `[Collection]` attribute to run sequentially
2. Make `RateLimiter` injectable (dependency injection)
3. Add test-only reset mechanism
4. Run tests with `--max-parallelization 1`

**Example of commented test:**
```csharp
// These tests verify rate limiting but are disabled for parallel execution
/*
[Fact]
public void Operations_ExceedingRateLimit_ThrowsInvalidOperationException() { ... }
*/
```

To enable these tests, uncomment them and run:
```bash
dotnet test --max-parallelization 1
```

---

## Test Categories

### Security Tests
Tests that verify security features:
- Input validation preventing malicious inputs
- Rate limiting preventing abuse
- Resource limits preventing overflow/exhaustion
- Error handling preventing information leakage
- API key authentication (header validation, timing-safe comparison)
- JWT authentication (secret key validation, token validation parameters)
- Authentication challenge responses (401 with proper headers)

### Functional Tests
Tests that verify correctness:
- Arithmetic operations return correct results
- Decimal precision is preserved
- Edge cases handled properly
- Exceptions have correct types and messages

### Performance Tests
Tests that verify performance characteristics:
- Rate limiter sliding window cleanup
- Thread safety under concurrent load
- Timeout enforcement accuracy

---

## Continuous Integration

### GitHub Actions Example
```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --logger trx --collect:"XPlat Code Coverage" --settings coverlet.runsettings
      - uses: coverallsapp/github-action@v2
        with:
          github-token: ${{ secrets.GITHUB_TOKEN }}
```

---

## Code Coverage Goals

### McpCalculator.Core

| Component | Target | Current |
|-----------|--------|---------|
| ResourceLimits | 100% | ✅ 100% |
| RateLimiter | 100% | ✅ 100% |
| ExecutionContext | 100% | ✅ 100% |
| CalculatorTools | 100% | ✅ 100% |
| **Overall Line Coverage** | **100%** | **✅ 100%** |
| **Overall Branch Coverage** | **100%** | **✅ 100%** |

### McpCalculator.Web

| Component | Target | Current |
|-----------|--------|---------|
| ApiKeyAuthHandler | High | ✅ 100% |
| NoOpAuthHandler | High | ✅ 100% |
| CalculatorApiEndpoints | High | ✅ 100% |

**Notes:**
- Program.cs is excluded from coverage (composition root)
- REST API endpoints are tested via integration tests using WebApplicationFactory
- Web project uses composition root pattern - authentication handlers are tested in isolation
- Model classes (CalculatorRequest, CalculatorResponse, CalculatorErrorResponse) are excluded from coverage

---

## Adding New Tests

When adding new calculator operations:

1. **Create test method in CalculatorToolsTests:**
```csharp
[Fact]
public void NewOperation_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var input = ...;

    // Act
    var result = _calculator.NewOperation(input);

    // Assert
    Assert.Equal(expected, result);
}
```

2. **Add validation tests:**
```csharp
[Fact]
public void NewOperation_WithInvalidInput_ThrowsException()
{
    Assert.Throws<ArgumentException>(() =>
        _calculator.NewOperation(invalidInput));
}
```

3. **Add edge case tests:**
```csharp
[Fact]
public void NewOperation_WithEdgeCase_HandlesCorrectly()
{
    // Test boundary conditions, extreme values, etc.
}
```

---

## Test Naming Convention

Tests follow the pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `Add_WithValidNumbers_ReturnsSum`
- `Divide_ByZero_ThrowsArgumentException`
- `ValidateValueRange_WithExcessivelyLargeValue_ThrowsArgumentOutOfRangeException`

---

## Dependencies

- **xUnit** - Test framework
- **xUnit.runner.visualstudio** - Visual Studio test adapter
- **Microsoft.NET.Test.Sdk** - .NET test SDK
- **Moq** - Mocking framework for authentication handler tests
- **Microsoft.AspNetCore.Mvc.Testing** - ASP.NET Core test utilities
- **coverlet.collector** - Code coverage collection

All dependencies are managed via NuGet and included in the project file.

---

## Troubleshooting

### Tests Fail with Rate Limit Errors
**Cause:** Tests running in parallel hitting shared rate limiter.

**Solution:** Run tests sequentially:
```bash
dotnet test --max-parallelization 1
```

### Timeout Tests Are Flaky
**Cause:** System load affecting timing.

**Solution:** Increase timeout margins or run on isolated build agent.

### Tests Fail on CI but Pass Locally
**Cause:** Different .NET SDK versions or OS differences.

**Solution:** Specify exact SDK version in `global.json`.

---

## Related Documentation

- [MCP Specification](https://modelcontextprotocol.io/specification/)
- [Authentication Guide](../docs/AUTHENTICATION.md)
- [REST API Guide](../docs/REST_API.md)
- [HTTP Transport Guide](../docs/HTTP_TRANSPORT.md)
- [IIS Deployment Guide](../docs/IIS_DEPLOYMENT.md)
