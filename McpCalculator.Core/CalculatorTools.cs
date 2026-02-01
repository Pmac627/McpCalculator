using ModelContextProtocol.Server;

namespace McpCalculator.Core
{
    /// <summary>
    /// Provides secure calculator operations with resource limits and rate limiting.
    /// All operations support numbers within ±1e15 and are rate limited to 100 requests per minute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements the MCP (Model Context Protocol) server tools for basic
    /// arithmetic operations. Each method is exposed as an MCP tool that can be called
    /// by AI assistants like Claude.
    /// </para>
    /// <para><b>Security Features:</b></para>
    /// <list type="bullet">
    ///   <item><b>Input Validation:</b> All inputs are validated for NaN, Infinity, and range limits</item>
    ///   <item><b>Rate Limiting:</b> Each operation is limited to 100 requests per minute</item>
    ///   <item><b>Resource Limits:</b> Results are validated to stay within safe bounds</item>
    /// </list>
    /// <para><b>Transport Agnostic:</b> These tools work with both stdio and HTTP transports.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // The tools are registered with the MCP server in Program.cs:
    /// builder.Services
    ///     .AddMcpServer()
    ///     .WithStdioServerTransport()  // or WithHttpTransport()
    ///     .WithTools&lt;CalculatorTools&gt;();
    /// </code>
    /// </example>
    [McpServerToolType]
    public sealed partial class CalculatorTools
    {
        /// <summary>
        /// Rate limiter: 100 requests per minute per operation.
        /// Each operation (Add, Subtract, etc.) has its own independent limit.
        /// </summary>
        private static readonly RateLimiter _rateLimiter = new(maxRequestsPerWindow: 100, windowDuration: TimeSpan.FromMinutes(1));

        /// <summary>
        /// Validates that a number is valid for calculation.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when value is NaN or Infinity.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value exceeds limits.</exception>
        private static void ValidateNumber(double value, string paramName)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException($"{paramName} is not a valid number (NaN)", paramName);
            }

            if (double.IsInfinity(value))
            {
                throw new ArgumentException($"{paramName} is infinity", paramName);
            }

            // Check resource limits
            ResourceLimits.ValidateValueRange(value, paramName);
        }

        /// <summary>
        /// Add two numbers together.
        /// </summary>
        /// <remarks>
        /// <para><b>Constraints:</b></para>
        /// <list type="bullet">
        ///   <item>Inputs and result must be within ±1e15 (chosen to prevent overflow while allowing large calculations)</item>
        ///   <item>Rate limited to 100 requests per minute PER OPERATION (add/subtract/multiply/divide each have separate limits)</item>
        ///   <item>Returns standard .NET double (IEEE 754 binary64) with 15-17 significant digits precision</item>
        /// </list>
        /// <para><b>Error behavior:</b> Throws exceptions on constraint violations (not null/error codes).</para>
        /// </remarks>
        /// <param name="a">First number (must be within ±1e15).</param>
        /// <param name="b">Second number (must be within ±1e15).</param>
        /// <returns>The sum of a and b as a double-precision floating-point number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when input values exceed ±1e15.</exception>
        /// <exception cref="InvalidOperationException">Thrown when result exceeds ±1e15 or rate limit (100/min) is exceeded.</exception>
        /// <example>
        /// <code>
        /// var tools = new CalculatorTools();
        /// double result = tools.Add(5.0, 3.0);  // Returns 8.0
        /// </code>
        /// </example>
        [McpServerTool]
        public partial double Add(double a, double b)
        {
            _rateLimiter.CheckRateLimit(nameof(Add));

            ValidateNumber(a, nameof(a));
            ValidateNumber(b, nameof(b));

            var result = a + b;
            ResourceLimits.ValidateResult(result, nameof(Add));

            return result;
        }

        /// <summary>
        /// Subtract the second number from the first.
        /// </summary>
        /// <remarks>
        /// <para><b>Constraints:</b></para>
        /// <list type="bullet">
        ///   <item>Inputs and result must be within ±1e15 (chosen to prevent overflow while allowing large calculations)</item>
        ///   <item>Rate limited to 100 requests per minute PER OPERATION (add/subtract/multiply/divide each have separate limits)</item>
        ///   <item>Returns standard .NET double (IEEE 754 binary64) with 15-17 significant digits precision</item>
        /// </list>
        /// <para><b>Error behavior:</b> Throws exceptions on constraint violations (not null/error codes).</para>
        /// </remarks>
        /// <param name="a">First number (must be within ±1e15).</param>
        /// <param name="b">Second number (must be within ±1e15).</param>
        /// <returns>The difference of a minus b as a double-precision floating-point number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when input values exceed ±1e15.</exception>
        /// <exception cref="InvalidOperationException">Thrown when result exceeds ±1e15 or rate limit (100/min) is exceeded.</exception>
        /// <example>
        /// <code>
        /// var tools = new CalculatorTools();
        /// double result = tools.Subtract(10.0, 4.0);  // Returns 6.0
        /// </code>
        /// </example>
        [McpServerTool]
        public partial double Subtract(double a, double b)
        {
            _rateLimiter.CheckRateLimit(nameof(Subtract));

            ValidateNumber(a, nameof(a));
            ValidateNumber(b, nameof(b));

            var result = a - b;
            ResourceLimits.ValidateResult(result, nameof(Subtract));

            return result;
        }

        /// <summary>
        /// Multiply two numbers.
        /// </summary>
        /// <remarks>
        /// <para><b>Constraints:</b></para>
        /// <list type="bullet">
        ///   <item>Inputs and result must be within ±1e15 (chosen to prevent overflow while allowing large calculations)</item>
        ///   <item>Rate limited to 100 requests per minute PER OPERATION (add/subtract/multiply/divide each have separate limits)</item>
        ///   <item>Returns standard .NET double (IEEE 754 binary64) with 15-17 significant digits precision</item>
        /// </list>
        /// <para><b>Error behavior:</b> Throws exceptions on constraint violations (not null/error codes).</para>
        /// </remarks>
        /// <param name="a">First number (must be within ±1e15).</param>
        /// <param name="b">Second number (must be within ±1e15).</param>
        /// <returns>The product of a and b as a double-precision floating-point number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when input values exceed ±1e15.</exception>
        /// <exception cref="InvalidOperationException">Thrown when result exceeds ±1e15 or rate limit (100/min) is exceeded.</exception>
        /// <example>
        /// <code>
        /// var tools = new CalculatorTools();
        /// double result = tools.Multiply(6.0, 7.0);  // Returns 42.0
        /// </code>
        /// </example>
        [McpServerTool]
        public partial double Multiply(double a, double b)
        {
            _rateLimiter.CheckRateLimit(nameof(Multiply));

            ValidateNumber(a, nameof(a));
            ValidateNumber(b, nameof(b));

            var result = a * b;
            ResourceLimits.ValidateResult(result, nameof(Multiply));

            return result;
        }

        /// <summary>
        /// Divide the first number by the second.
        /// </summary>
        /// <remarks>
        /// <para><b>Constraints:</b></para>
        /// <list type="bullet">
        ///   <item>Inputs and result must be within ±1e15 (chosen to prevent overflow while allowing large calculations)</item>
        ///   <item>Denominator cannot be zero and absolute value must be ≥ 1e-10 (prevents divide-by-near-zero overflow)</item>
        ///   <item>Rate limited to 100 requests per minute PER OPERATION (add/subtract/multiply/divide each have separate limits)</item>
        ///   <item>Returns standard .NET double (IEEE 754 binary64) with 15-17 significant digits precision</item>
        /// </list>
        /// <para><b>Error behavior:</b> Throws exceptions on constraint violations (not null/error codes).</para>
        /// </remarks>
        /// <param name="a">Numerator (must be within ±1e15).</param>
        /// <param name="b">Denominator (must be within ±1e15, cannot be zero, absolute value ≥ 1e-10).</param>
        /// <returns>The quotient of a divided by b as a double-precision floating-point number.</returns>
        /// <exception cref="ArgumentException">Thrown when denominator is zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when input values exceed ±1e15 or denominator is too small (|b| &lt; 1e-10).</exception>
        /// <exception cref="InvalidOperationException">Thrown when result exceeds ±1e15 or rate limit (100/min) is exceeded.</exception>
        /// <example>
        /// <code>
        /// var tools = new CalculatorTools();
        /// double result = tools.Divide(20.0, 4.0);  // Returns 5.0
        /// </code>
        /// </example>
        [McpServerTool]
        public partial double Divide(double a, double b)
        {
            _rateLimiter.CheckRateLimit(nameof(Divide));

            ValidateNumber(a, nameof(a));
            ValidateNumber(b, nameof(b));

            // Special validation for division denominator
            ResourceLimits.ValidateDivisionDenominator(b, nameof(b));

            var result = a / b;
            ResourceLimits.ValidateResult(result, nameof(Divide));

            return result;
        }
    }
}
