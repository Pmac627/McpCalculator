using System.Collections.Concurrent;

namespace McpCalculator.Core
{
    /// <summary>
    /// Thread-safe rate limiter using sliding window algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rate limiter uses a sliding window algorithm to track requests over time.
    /// Unlike fixed window algorithms, sliding windows provide smoother rate limiting
    /// without allowing bursts at window boundaries.
    /// </para>
    /// <para>
    /// Each operation (e.g., Add, Subtract) is tracked independently, allowing for
    /// per-operation rate limiting.
    /// </para>
    /// <para><b>Thread Safety:</b> This class is fully thread-safe and can be used
    /// from multiple concurrent threads.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a rate limiter: 100 requests per minute
    /// var limiter = new RateLimiter(
    ///     maxRequestsPerWindow: 100,
    ///     windowDuration: TimeSpan.FromMinutes(1)
    /// );
    ///
    /// // Check rate limit before each operation
    /// limiter.CheckRateLimit("Add");  // Throws if limit exceeded
    /// </code>
    /// </example>
    public sealed class RateLimiter
    {
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestHistory = new();
        private readonly int _maxRequestsPerWindow;
        private readonly TimeSpan _windowDuration;

        /// <summary>
        /// Creates a new rate limiter.
        /// </summary>
        /// <param name="maxRequestsPerWindow">Maximum number of requests allowed in the time window.</param>
        /// <param name="windowDuration">Duration of the sliding time window.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="maxRequestsPerWindow"/> is not positive or
        /// <paramref name="windowDuration"/> is not positive.
        /// </exception>
        /// <example>
        /// <code>
        /// // 100 requests per minute
        /// var limiter = new RateLimiter(100, TimeSpan.FromMinutes(1));
        ///
        /// // 10 requests per second
        /// var strictLimiter = new RateLimiter(10, TimeSpan.FromSeconds(1));
        /// </code>
        /// </example>
        public RateLimiter(int maxRequestsPerWindow, TimeSpan windowDuration)
        {
            if (maxRequestsPerWindow <= 0)
            {
                throw new ArgumentException("Max requests must be positive", nameof(maxRequestsPerWindow));
            }

            if (windowDuration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Window duration must be positive", nameof(windowDuration));
            }

            _maxRequestsPerWindow = maxRequestsPerWindow;
            _windowDuration = windowDuration;
        }

        /// <summary>
        /// Checks if a request should be allowed for the given operation.
        /// </summary>
        /// <param name="operationName">Name of the operation being rate limited.</param>
        /// <exception cref="InvalidOperationException">Thrown when rate limit is exceeded.</exception>
        /// <remarks>
        /// <para>
        /// This method is thread-safe and can be called concurrently from multiple threads.
        /// If the rate limit is exceeded, the exception message includes the retry-after duration.
        /// </para>
        /// <para>
        /// Each operation name has its own independent rate limit counter. For example,
        /// "Add" and "Multiply" operations are tracked separately.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     limiter.CheckRateLimit("Add");
        ///     // Proceed with operation
        /// }
        /// catch (InvalidOperationException ex)
        /// {
        ///     // Rate limit exceeded - ex.Message contains retry information
        /// }
        /// </code>
        /// </example>
        public void CheckRateLimit(string operationName)
        {
            var now = DateTime.UtcNow;
            var cutoff = now - _windowDuration;

            var queue = _requestHistory.GetOrAdd(operationName, _ => new Queue<DateTime>());

            lock (queue)
            {
                // Remove old requests outside the window
                while (queue.Count > 0 && queue.Peek() < cutoff)
                {
                    queue.Dequeue();
                }

                // Check if we've exceeded the limit
                if (queue.Count >= _maxRequestsPerWindow)
                {
                    var oldestRequest = queue.Peek();
                    var retryAfter = oldestRequest + _windowDuration - now;

                    throw new InvalidOperationException(
                        $"Rate limit exceeded for '{operationName}'. " +
                        $"Maximum {_maxRequestsPerWindow} requests per {_windowDuration.TotalSeconds} seconds. " +
                        $"Retry after {retryAfter.TotalSeconds:F1} seconds.");
                }

                // Record this request
                queue.Enqueue(now);
            }
        }

        /// <summary>
        /// Gets the current request count for an operation within the window.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>Number of requests in the current window.</returns>
        /// <remarks>
        /// This method is useful for monitoring and debugging rate limit status.
        /// It does not count as a request itself.
        /// </remarks>
        /// <example>
        /// <code>
        /// int currentCount = limiter.GetCurrentCount("Add");
        /// Console.WriteLine($"Add operation: {currentCount}/{maxRequests} requests used");
        /// </code>
        /// </example>
        public int GetCurrentCount(string operationName)
        {
            if (!_requestHistory.TryGetValue(operationName, out var queue))
            {
                return 0;
            }

            var cutoff = DateTime.UtcNow - _windowDuration;

            lock (queue)
            {
                // Remove old requests
                while (queue.Count > 0 && queue.Peek() < cutoff)
                {
                    queue.Dequeue();
                }

                return queue.Count;
            }
        }

        /// <summary>
        /// Clears all rate limit history.
        /// </summary>
        /// <remarks>
        /// This method resets all operation counters. Use with caution in production
        /// as it allows previously rate-limited clients to immediately make new requests.
        /// Primarily useful for testing.
        /// </remarks>
        public void Clear()
        {
            _requestHistory.Clear();
        }
    }
}
