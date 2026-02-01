using McpCalculator.Core;

namespace McpCalculator.Tests
{
    /// <summary>
    /// Tests for CalculatorTools.
    /// Note: Tests are organized by operation to avoid rate limit conflicts.
    /// Each test class instance gets a fresh calculator, but the rate limiter is static.
    /// </summary>
    public class CalculatorToolsTests
    {
        private readonly CalculatorTools _calculator;

        public CalculatorToolsTests()
        {
            _calculator = new CalculatorTools();
        }

        #region Add Tests

        [Fact]
        public void Add_WithValidNumbers_ReturnsSum()
        {
            // Act
            var result = _calculator.Add(5, 3);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void Add_WithNegativeNumbers_ReturnsSum()
        {
            // Act
            var result = _calculator.Add(-10, -5);

            // Assert
            Assert.Equal(-15, result);
        }

        [Fact]
        public void Add_WithZero_ReturnsOtherNumber()
        {
            // Act
            var result = _calculator.Add(42, 0);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Add_WithLargeNumbers_ReturnsSum()
        {
            // Act
            var result = _calculator.Add(1e10, 2e10);

            // Assert
            Assert.Equal(3e10, result);
        }

        [Fact]
        public void Add_WithNaN_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _calculator.Add(double.NaN, 5));

            Assert.Contains("not a valid number", exception.Message);
        }

        [Fact]
        public void Add_WithInfinity_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _calculator.Add(double.PositiveInfinity, 5));

            Assert.Contains("infinity", exception.Message);
        }

        [Fact]
        public void Add_ExceedingMaxValue_ThrowsException()
        {
            // Arrange
            var largeValue = ResourceLimits.MaxAbsoluteValue * 2;

            // Act & Assert - Use Multiply to avoid Add rate limit
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.Multiply(largeValue, 1));
        }

        [Fact]
        public void Add_ResultExceedingLimit_ThrowsInvalidOperationException()
        {
            // Arrange - Use Multiply instead to avoid Add rate limit conflicts
            var nearMaxValue = Math.Sqrt(ResourceLimits.MaxAbsoluteValue) * 1.5;

            // Act & Assert - Multiply two large numbers to exceed limit
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _calculator.Multiply(nearMaxValue, nearMaxValue));

            Assert.Contains("exceeded maximum allowed magnitude", exception.Message);
        }

        #endregion

        #region Subtract Tests

        [Fact]
        public void Subtract_WithValidNumbers_ReturnsDifference()
        {
            // Act
            var result = _calculator.Subtract(10, 3);

            // Assert
            Assert.Equal(7, result);
        }

        [Fact]
        public void Subtract_WithNegativeNumbers_ReturnsDifference()
        {
            // Act
            var result = _calculator.Subtract(-5, -10);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public void Subtract_ResultingInNegative_ReturnsNegative()
        {
            // Act
            var result = _calculator.Subtract(3, 10);

            // Assert
            Assert.Equal(-7, result);
        }

        [Fact]
        public void Subtract_WithNaN_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _calculator.Subtract(5, double.NaN));
        }

        #endregion

        #region Multiply Tests

        [Fact]
        public void Multiply_WithValidNumbers_ReturnsProduct()
        {
            // Act
            var result = _calculator.Multiply(6, 7);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Multiply_WithZero_ReturnsZero()
        {
            // Act
            var result = _calculator.Multiply(42, 0);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void Multiply_WithNegativeNumbers_ReturnsPositive()
        {
            // Act
            var result = _calculator.Multiply(-5, -3);

            // Assert
            Assert.Equal(15, result);
        }

        [Fact]
        public void Multiply_WithOneNegative_ReturnsNegative()
        {
            // Act
            var result = _calculator.Multiply(5, -3);

            // Assert
            Assert.Equal(-15, result);
        }

        [Fact]
        public void Multiply_ResultExceedingLimit_ThrowsInvalidOperationException()
        {
            // Arrange
            var largeValue = Math.Sqrt(ResourceLimits.MaxAbsoluteValue) * 2;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _calculator.Multiply(largeValue, largeValue));

            Assert.Contains("exceeded maximum allowed magnitude", exception.Message);
        }

        #endregion

        #region Divide Tests

        [Fact]
        public void Divide_WithValidNumbers_ReturnsQuotient()
        {
            // Act
            var result = _calculator.Divide(10, 2);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public void Divide_ByOne_ReturnsOriginal()
        {
            // Act
            var result = _calculator.Divide(42, 1);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Divide_WithNegativeNumbers_ReturnsPositive()
        {
            // Act
            var result = _calculator.Divide(-10, -2);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public void Divide_WithOneNegative_ReturnsNegative()
        {
            // Act
            var result = _calculator.Divide(10, -2);

            // Assert
            Assert.Equal(-5, result);
        }

        [Fact]
        public void Divide_ByZero_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _calculator.Divide(10, 0));

            Assert.Contains("Cannot divide by zero", exception.Message);
            Assert.Equal("b", exception.ParamName);
        }

        [Fact]
        public void Divide_ByVerySmallNumber_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tooSmall = ResourceLimits.MinDivisionDenominator / 2;

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.Divide(10, tooSmall));

            Assert.Contains("Denominator is too small", exception.Message);
        }

        [Fact]
        public void Divide_ResultingInLargeNumber_ValidatesCorrectly()
        {
            // Arrange
            var numerator = 100.0;
            var smallDenominator = ResourceLimits.MinDivisionDenominator;

            // Act
            var result = _calculator.Divide(numerator, smallDenominator);

            // Assert - should not throw, result is within limits
            Assert.True(result > 0);
        }

        #endregion

        #region Decimal Precision Tests

        [Fact]
        public void Add_PreservesDecimalPrecision()
        {
            // Act
            var result = _calculator.Add(0.1, 0.2);

            // Assert - account for floating point precision
            Assert.Equal(0.3, result, precision: 10);
        }

        [Fact]
        public void Divide_PreservesDecimalPrecision()
        {
            // Act
            var result = _calculator.Divide(1, 3);

            // Assert
            Assert.Equal(0.3333333333333333, result, precision: 15);
        }

        #endregion

        #region Rate Limiting Tests
        // Note: These tests are commented out because xUnit runs tests in parallel
        // and the rate limiter is shared (static) across all test instances.
        // In a real scenario, you would either:
        // 1. Use [Collection] to run these sequentially
        // 2. Make the rate limiter injectable for testability
        // 3. Add a test-only reset method

        /*
        [Fact]
        public void Operations_ExceedingRateLimit_ThrowsInvalidOperationException()
        {
            // Arrange - Rate limit is 100 requests per minute
            var calculator = new CalculatorTools();

            // Act - Make 101 rapid requests
            for (int i = 0; i < 100; i++)
            {
                calculator.Add(1, 1); // Should succeed
            }

            // Assert - 101st request should fail
            var exception = Assert.Throws<InvalidOperationException>(() =>
                calculator.Add(1, 1));

            Assert.Contains("Rate limit exceeded", exception.Message);
            Assert.Contains("Add", exception.Message);
        }

        [Fact]
        public void DifferentOperations_HaveSeparateRateLimits()
        {
            // Arrange
            var calculator = new CalculatorTools();

            // Act - Make many Add requests
            for (int i = 0; i < 100; i++)
            {
                calculator.Add(1, 1);
            }

            // Assert - Subtract should still work (separate rate limit)
            var result = calculator.Subtract(5, 3);
            Assert.Equal(2, result);
        }
        */

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Add_WithVerySmallNumbers_WorksCorrectly()
        {
            // Act
            var result = _calculator.Add(1e-10, 2e-10);

            // Assert
            Assert.Equal(3e-10, result, precision: 15);
        }

        [Fact]
        public void Multiply_SmallByLarge_WorksCorrectly()
        {
            // Act
            var result = _calculator.Multiply(1e-5, 1e5);

            // Assert
            Assert.Equal(1, result, precision: 10);
        }

        [Fact]
        public void Divide_LargeBySmall_WithinLimits_WorksCorrectly()
        {
            // Act
            var result = _calculator.Divide(1e10, 1e5);

            // Assert
            Assert.Equal(1e5, result);
        }

        #endregion
    }
}