using McpCalculator.Core;

namespace McpCalculator.Tests
{
    public class ResourceLimitsTests
    {
        [Fact]
        public void ValidateValueRange_WithValidValue_DoesNotThrow()
        {
            // Arrange
            var validValue = 1000.0;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateValueRange(validValue, "testParam"));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateValueRange_WithMaxAllowedValue_DoesNotThrow()
        {
            // Arrange
            var maxValue = ResourceLimits.MaxAbsoluteValue;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateValueRange(maxValue, "testParam"));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateValueRange_WithNegativeMaxAllowedValue_DoesNotThrow()
        {
            // Arrange
            var minValue = -ResourceLimits.MaxAbsoluteValue;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateValueRange(minValue, "testParam"));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateValueRange_WithExcessivelyLargeValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tooLargeValue = ResourceLimits.MaxAbsoluteValue * 2;

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ResourceLimits.ValidateValueRange(tooLargeValue, "testParam"));

            Assert.Contains("exceeds maximum allowed magnitude", exception.Message);
            Assert.Equal("testParam", exception.ParamName);
        }

        [Fact]
        public void ValidateValueRange_WithExcessivelySmallValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tooSmallValue = -ResourceLimits.MaxAbsoluteValue * 2;

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ResourceLimits.ValidateValueRange(tooSmallValue, "testParam"));

            Assert.Contains("exceeds maximum allowed magnitude", exception.Message);
            Assert.Equal("testParam", exception.ParamName);
        }

        [Fact]
        public void ValidateDivisionDenominator_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var zero = 0.0;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                ResourceLimits.ValidateDivisionDenominator(zero, "denominator"));

            Assert.Contains("Cannot divide by zero", exception.Message);
            Assert.Equal("denominator", exception.ParamName);
        }

        [Fact]
        public void ValidateDivisionDenominator_WithValidValue_DoesNotThrow()
        {
            // Arrange
            var validDenominator = 10.0;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateDivisionDenominator(validDenominator, "denominator"));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateDivisionDenominator_WithMinimumAllowedValue_DoesNotThrow()
        {
            // Arrange
            var minDenominator = ResourceLimits.MinDivisionDenominator;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateDivisionDenominator(minDenominator, "denominator"));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateDivisionDenominator_WithTooSmallValue_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tooSmall = ResourceLimits.MinDivisionDenominator / 2;

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                ResourceLimits.ValidateDivisionDenominator(tooSmall, "denominator"));

            Assert.Contains("Denominator is too small", exception.Message);
            Assert.Equal("denominator", exception.ParamName);
        }

        [Fact]
        public void ValidateResult_WithValidResult_DoesNotThrow()
        {
            // Arrange
            var validResult = 42.0;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateResult(validResult, "Add"));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateResult_WithInfinity_ThrowsInvalidOperationException()
        {
            // Arrange
            var infinityResult = double.PositiveInfinity;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ResourceLimits.ValidateResult(infinityResult, "Multiply"));

            Assert.Contains("resulted in infinity", exception.Message);
            Assert.Contains("Multiply", exception.Message);
        }

        [Fact]
        public void ValidateResult_WithNaN_ThrowsInvalidOperationException()
        {
            // Arrange
            var nanResult = double.NaN;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ResourceLimits.ValidateResult(nanResult, "Divide"));

            Assert.Contains("resulted in NaN", exception.Message);
            Assert.Contains("Divide", exception.Message);
        }

        [Fact]
        public void ValidateResult_WithExcessivelyLargeResult_ThrowsInvalidOperationException()
        {
            // Arrange
            var tooLargeResult = ResourceLimits.MaxAbsoluteValue * 2;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ResourceLimits.ValidateResult(tooLargeResult, "Operation"));

            Assert.Contains("exceeded maximum allowed magnitude", exception.Message);
            Assert.Contains("Operation", exception.Message);
        }

        [Fact]
        public void ValidateResult_WithMaxAllowedValue_DoesNotThrow()
        {
            // Arrange
            var maxResult = ResourceLimits.MaxAbsoluteValue;

            // Act & Assert
            var exception = Record.Exception(() =>
                ResourceLimits.ValidateResult(maxResult, "Operation"));

            Assert.Null(exception);
        }
    }
}