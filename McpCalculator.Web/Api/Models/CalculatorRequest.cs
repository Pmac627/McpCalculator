using System.Diagnostics.CodeAnalysis;

namespace McpCalculator.Web.Api.Models
{
    /// <summary>
    /// Request model for calculator operations that take two operands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is used by all four basic calculator operations:
    /// Add, Subtract, Multiply, and Divide.
    /// </para>
    /// <para><b>Validation:</b></para>
    /// <list type="bullet">
    ///   <item>Both operands must be within ±1e15</item>
    ///   <item>Neither operand can be NaN or Infinity</item>
    ///   <item>For division, B cannot be zero or have absolute value less than 1e-10</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// JSON request body:
    /// <code>
    /// {
    ///   "a": 10.5,
    ///   "b": 3.2
    /// }
    /// </code>
    /// </example>
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public record CalculatorRequest(double A, double B);
}