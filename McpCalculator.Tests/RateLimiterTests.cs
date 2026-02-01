using McpCalculator.Core;

namespace McpCalculator.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var rateLimiter = new RateLimiter(
                maxRequestsPerWindow: 10,
                windowDuration: TimeSpan.FromSeconds(60));

            // Assert
            Assert.NotNull(rateLimiter);
        }

        [Fact]
        public void Constructor_WithZeroMaxRequests_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new RateLimiter(0, TimeSpan.FromSeconds(60)));

            Assert.Contains("Max requests must be positive", exception.Message);
        }

        [Fact]
        public void Constructor_WithNegativeMaxRequests_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new RateLimiter(-5, TimeSpan.FromSeconds(60)));

            Assert.Contains("Max requests must be positive", exception.Message);
        }

        [Fact]
        public void Constructor_WithZeroWindowDuration_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new RateLimiter(10, TimeSpan.Zero));

            Assert.Contains("Window duration must be positive", exception.Message);
        }

        [Fact]
        public void CheckRateLimit_WithinLimit_DoesNotThrow()
        {
            // Arrange
            var rateLimiter = new RateLimiter(5, TimeSpan.FromSeconds(1));

            // Act & Assert - should allow 5 requests
            for (int i = 0; i < 5; i++)
            {
                var exception = Record.Exception(() =>
                    rateLimiter.CheckRateLimit("testOperation"));

                Assert.Null(exception);
            }
        }

        [Fact]
        public void CheckRateLimit_ExceedingLimit_ThrowsInvalidOperationException()
        {
            // Arrange
            var rateLimiter = new RateLimiter(3, TimeSpan.FromSeconds(10));

            // Act - make 3 allowed requests
            for (int i = 0; i < 3; i++)
            {
                rateLimiter.CheckRateLimit("testOperation");
            }

            // Assert - 4th request should throw
            var exception = Assert.Throws<InvalidOperationException>(() =>
                rateLimiter.CheckRateLimit("testOperation"));

            Assert.Contains("Rate limit exceeded", exception.Message);
            Assert.Contains("testOperation", exception.Message);
            Assert.Contains("3 requests per 10 seconds", exception.Message);
        }

        [Fact]
        public void CheckRateLimit_DifferentOperations_TrackedSeparately()
        {
            // Arrange
            var rateLimiter = new RateLimiter(2, TimeSpan.FromSeconds(10));

            // Act - 2 requests for operation A
            rateLimiter.CheckRateLimit("operationA");
            rateLimiter.CheckRateLimit("operationA");

            // Assert - operation A should be at limit
            var exceptionA = Assert.Throws<InvalidOperationException>(() =>
                rateLimiter.CheckRateLimit("operationA"));

            Assert.Contains("operationA", exceptionA.Message);

            // Act & Assert - operation B should still be allowed
            var exceptionB = Record.Exception(() =>
                rateLimiter.CheckRateLimit("operationB"));

            Assert.Null(exceptionB);
        }

        [Fact]
        public void CheckRateLimit_AfterWindowExpires_AllowsNewRequests()
        {
            // Arrange
            var rateLimiter = new RateLimiter(2, TimeSpan.FromMilliseconds(100));

            // Act - make 2 requests (at limit)
            rateLimiter.CheckRateLimit("testOperation");
            rateLimiter.CheckRateLimit("testOperation");

            // Verify we're at the limit
            Assert.Throws<InvalidOperationException>(() =>
                rateLimiter.CheckRateLimit("testOperation"));

            // Wait for window to expire
            Thread.Sleep(150);

            // Assert - should allow new requests after window expires
            var exception = Record.Exception(() =>
                rateLimiter.CheckRateLimit("testOperation"));

            Assert.Null(exception);
        }

        [Fact]
        public void GetCurrentCount_WithNoRequests_ReturnsZero()
        {
            // Arrange
            var rateLimiter = new RateLimiter(10, TimeSpan.FromSeconds(1));

            // Act
            var count = rateLimiter.GetCurrentCount("testOperation");

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetCurrentCount_AfterRequests_ReturnsCorrectCount()
        {
            // Arrange
            var rateLimiter = new RateLimiter(10, TimeSpan.FromSeconds(10));

            // Act
            rateLimiter.CheckRateLimit("testOperation");
            rateLimiter.CheckRateLimit("testOperation");
            rateLimiter.CheckRateLimit("testOperation");

            var count = rateLimiter.GetCurrentCount("testOperation");

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void GetCurrentCount_AfterWindowExpires_ReturnsZero()
        {
            // Arrange
            var rateLimiter = new RateLimiter(5, TimeSpan.FromMilliseconds(100));

            // Act
            rateLimiter.CheckRateLimit("testOperation");
            rateLimiter.CheckRateLimit("testOperation");

            // Verify count before expiry
            Assert.Equal(2, rateLimiter.GetCurrentCount("testOperation"));

            // Wait for window to expire
            Thread.Sleep(150);

            // Assert
            Assert.Equal(0, rateLimiter.GetCurrentCount("testOperation"));
        }

        [Fact]
        public void Clear_RemovesAllHistory()
        {
            // Arrange
            var rateLimiter = new RateLimiter(5, TimeSpan.FromSeconds(10));

            rateLimiter.CheckRateLimit("operation1");
            rateLimiter.CheckRateLimit("operation2");
            rateLimiter.CheckRateLimit("operation1");

            Assert.Equal(2, rateLimiter.GetCurrentCount("operation1"));
            Assert.Equal(1, rateLimiter.GetCurrentCount("operation2"));

            // Act
            rateLimiter.Clear();

            // Assert
            Assert.Equal(0, rateLimiter.GetCurrentCount("operation1"));
            Assert.Equal(0, rateLimiter.GetCurrentCount("operation2"));
        }

        [Fact]
        public async Task CheckRateLimit_ConcurrentRequests_ThreadSafe()
        {
            // Arrange
            var rateLimiter = new RateLimiter(100, TimeSpan.FromSeconds(1));
            var successCount = 0;
            var tasks = new List<Task>();

            // Act - spawn 50 concurrent tasks making 2 requests each (100 total)
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        rateLimiter.CheckRateLimit("concurrent");
                        Interlocked.Increment(ref successCount);

                        rateLimiter.CheckRateLimit("concurrent");
                        Interlocked.Increment(ref successCount);
                    }
                    catch (InvalidOperationException)
                    {
                        // Rate limit exceeded - expected for some threads
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - should allow exactly 100 requests (the limit)
            Assert.Equal(100, successCount);
        }
    }
}