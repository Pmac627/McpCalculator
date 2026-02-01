using System.Diagnostics.CodeAnalysis;

namespace McpCalculator.Web.Api.Models
{
    /// <summary>
    /// Response model for calculator errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returned when a calculation fails due to validation errors,
    /// rate limiting, or other issues.
    /// </para>
    /// <para><b>Common error types:</b></para>
    /// <list type="bullet">
    ///   <item><b>ValidationError:</b> Invalid input values (NaN, Infinity, out of range)</item>
    ///   <item><b>DivisionError:</b> Division by zero or near-zero denominator</item>
    ///   <item><b>RateLimitError:</b> Too many requests (100/minute exceeded)</item>
    ///   <item><b>OverflowError:</b> Result exceeds allowed range (±1e15)</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// JSON error response:
    /// <code>
    /// {
    ///   "error": "ValidationError",
    ///   "message": "Value exceeds maximum allowed: 1E+15",
    ///   "operation": "Add"
    /// }
    /// </code>
    /// </example>
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public record CalculatorErrorResponse(string Error, string Message, string Operation);
}