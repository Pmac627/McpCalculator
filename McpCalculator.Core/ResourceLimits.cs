namespace McpCalculator.Core
{
    /// <summary>
    /// Defines resource limits for calculator operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides validation constants and methods to ensure calculator
    /// operations stay within safe bounds. All limits are designed to:
    /// </para>
    /// <list type="bullet">
    ///   <item>Prevent overflow and underflow conditions</item>
    ///   <item>Maintain numerical precision (IEEE 754 binary64)</item>
    ///   <item>Protect against resource exhaustion attacks</item>
    /// </list>
    /// </remarks>
    public static class ResourceLimits
    {
        /// <summary>
        /// Maximum absolute value allowed for input numbers.
        /// Prevents overflow and extremely large computations.
        /// </summary>
        /// <remarks>
        /// Set to 1e15 (1 quadrillion) to stay well within the range of
        /// IEEE 754 double-precision floating-point numbers while preventing
        /// overflow in multiplication operations.
        /// </remarks>
        public const double MaxAbsoluteValue = 1e15; // 1 quadrillion

        /// <summary>
        /// Minimum absolute value for division denominators (excluding zero).
        /// Prevents division by very small numbers that could cause overflow.
        /// </summary>
        /// <remarks>
        /// Set to 1e-10 to prevent results that exceed <see cref="MaxAbsoluteValue"/>
        /// when dividing by very small numbers.
        /// </remarks>
        public const double MinDivisionDenominator = 1e-10;

        /// <summary>
        /// Maximum number of significant digits to preserve precision.
        /// </summary>
        /// <remarks>
        /// IEEE 754 binary64 (double) provides 15-17 significant decimal digits
        /// of precision. We use 15 as a conservative limit.
        /// </remarks>
        public const int MaxSignificantDigits = 15;

        /// <summary>
        /// Validates that a value is within acceptable resource limits.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value exceeds limits.</exception>
        /// <example>
        /// <code>
        /// // Valid usage
        /// ResourceLimits.ValidateValueRange(1000.0, "input");
        ///
        /// // Throws ArgumentOutOfRangeException
        /// ResourceLimits.ValidateValueRange(1e16, "input");
        /// </code>
        /// </example>
        public static void ValidateValueRange(double value, string paramName)
        {
            var absoluteValue = Math.Abs(value);

            if (absoluteValue > MaxAbsoluteValue)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    $"Value exceeds maximum allowed magnitude of {MaxAbsoluteValue:E}. " +
                    $"This limit prevents resource exhaustion and overflow errors.");
            }
        }

        /// <summary>
        /// Validates that a division denominator is safe.
        /// </summary>
        /// <param name="denominator">The denominator value.</param>
        /// <param name="paramName">Parameter name for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when denominator is zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when denominator is too small.</exception>
        /// <example>
        /// <code>
        /// // Valid usage
        /// ResourceLimits.ValidateDivisionDenominator(5.0, "divisor");
        ///
        /// // Throws ArgumentException (division by zero)
        /// ResourceLimits.ValidateDivisionDenominator(0, "divisor");
        ///
        /// // Throws ArgumentOutOfRangeException (too small)
        /// ResourceLimits.ValidateDivisionDenominator(1e-15, "divisor");
        /// </code>
        /// </example>
        public static void ValidateDivisionDenominator(double denominator, string paramName)
        {
            if (denominator == 0)
            {
                throw new ArgumentException("Cannot divide by zero", paramName);
            }

            var absoluteValue = Math.Abs(denominator);

            if (absoluteValue < MinDivisionDenominator)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    denominator,
                    $"Denominator is too small (less than {MinDivisionDenominator:E}). " +
                    $"Division by very small numbers can cause overflow.");
            }
        }

        /// <summary>
        /// Checks if a result is within acceptable bounds.
        /// </summary>
        /// <param name="result">The computed result.</param>
        /// <param name="operationName">Name of the operation for error messages.</param>
        /// <exception cref="InvalidOperationException">Thrown when result exceeds limits.</exception>
        /// <example>
        /// <code>
        /// double result = a * b;
        /// ResourceLimits.ValidateResult(result, "Multiply");
        /// return result;
        /// </code>
        /// </example>
        public static void ValidateResult(double result, string operationName)
        {
            if (double.IsInfinity(result))
            {
                throw new InvalidOperationException(
                    $"{operationName} resulted in infinity. " +
                    $"This indicates the result exceeded representable values.");
            }

            if (double.IsNaN(result))
            {
                throw new InvalidOperationException(
                    $"{operationName} resulted in NaN (Not a Number). " +
                    $"This indicates an invalid mathematical operation.");
            }

            var absoluteValue = Math.Abs(result);

            if (absoluteValue > MaxAbsoluteValue)
            {
                throw new InvalidOperationException(
                    $"{operationName} exceeded maximum allowed magnitude of {MaxAbsoluteValue:E}. " +
                    $"Result: {result:E}");
            }
        }
    }
}
