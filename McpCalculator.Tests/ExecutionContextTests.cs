using McpCalculator.Core;
using CalcExecutionContext = McpCalculator.Core.ExecutionContext;

namespace McpCalculator.Tests
{
    public class ExecutionContextTests
    {
        [Fact]
        public void Constructor_WithValidTimeout_CreatesInstance()
        {
            // Arrange & Act
            var context = new CalcExecutionContext(TimeSpan.FromSeconds(1));

            // Assert
            Assert.NotNull(context);
        }

        [Fact]
        public void Constructor_WithZeroTimeout_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CalcExecutionContext(TimeSpan.Zero));

            Assert.Contains("Timeout must be positive", exception.Message);
        }

        [Fact]
        public void Constructor_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CalcExecutionContext(TimeSpan.FromSeconds(-1)));

            Assert.Contains("Timeout must be positive", exception.Message);
        }

        [Fact]
        public void Execute_WithFastOperation_ReturnsResult()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromSeconds(1));

            // Act
            var result = context.Execute(() => 2 + 2, "Addition");

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void Execute_WithSlowOperation_ThrowsTimeoutException()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            var exception = Assert.Throws<TimeoutException>(() =>
                context.Execute(() =>
                {
                    Thread.Sleep(500); // Sleep longer than timeout
                    return 42;
                }, "SlowOperation"));

            Assert.Contains("SlowOperation", exception.Message);
            Assert.Contains("exceeded timeout", exception.Message);
        }

        [Fact]
        public void Execute_VoidOperation_CompletesSuccessfully()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromSeconds(1));
            var executed = false;

            // Act
            context.Execute(() => { executed = true; }, "VoidOperation");

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public void Execute_VoidOperationTimeout_ThrowsTimeoutException()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            var exception = Assert.Throws<TimeoutException>(() =>
                context.Execute(() =>
                {
                    Thread.Sleep(500);
                }, "SlowVoidOperation"));

            Assert.Contains("SlowVoidOperation", exception.Message);
        }

        [Fact]
        public void Execute_WithException_PropagatesException()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromSeconds(1));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                context.Execute<int>(() =>
                {
                    throw new InvalidOperationException("Test exception");
                }, "FailingOperation"));

            Assert.Equal("Test exception", exception.Message);
        }

        [Fact]
        public void Execute_MultipleOperations_EachHasOwnTimeout()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromMilliseconds(200));

            // Act & Assert - first operation succeeds
            var result1 = context.Execute(() =>
            {
                Thread.Sleep(50);
                return 1;
            }, "Operation1");

            Assert.Equal(1, result1);

            // Second operation succeeds
            var result2 = context.Execute(() =>
            {
                Thread.Sleep(50);
                return 2;
            }, "Operation2");

            Assert.Equal(2, result2);

            // Third operation times out
            Assert.Throws<TimeoutException>(() =>
                context.Execute(() =>
                {
                    Thread.Sleep(300);
                    return 3;
                }, "Operation3"));
        }

        [Fact]
        public void Execute_WithOperationThrowingTaskCanceledException_WrapsInTimeoutException()
        {
            // Arrange
            var context = new CalcExecutionContext(TimeSpan.FromSeconds(1));

            // Act & Assert - Operation that explicitly throws TaskCanceledException
            var exception = Assert.Throws<TimeoutException>(() =>
                context.Execute(() =>
                {
                    // Simulate an operation that throws TaskCanceledException
                    // (e.g., from an async operation that was cancelled)
                    throw new TaskCanceledException("Operation was cancelled");
                }, "CancelledOperation"));

            // Verify the exception message and inner exception
            Assert.Contains("CancelledOperation", exception.Message);
            Assert.Contains("was cancelled due to timeout", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.IsType<TaskCanceledException>(exception.InnerException);
        }
    }
}