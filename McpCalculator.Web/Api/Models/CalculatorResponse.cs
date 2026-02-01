using System.Diagnostics.CodeAnalysis;

namespace McpCalculator.Web.Api.Models
{
    /// <summary>
    /// Response model for successful calculator operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returned when a calculation completes successfully.
    /// The result is always a double-precision floating-point number.
    /// </para>
    /// </remarks>
    /// <example>
    /// JSON response:
    /// <code>
    /// {
    ///   "result": 13.7,
    ///   "operation": "Add"
    /// }
    /// </code>
    /// </example>
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public record CalculatorResponse(double Result, string Operation);
}