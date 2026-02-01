namespace McpCalculator.Core
{
    /// <summary>
    /// Provides execution context with timeout support for operations.
    /// Acts as a lightweight sandbox to prevent long-running operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class wraps operations with timeout enforcement, ensuring that
    /// no single operation can run indefinitely. This is particularly important
    /// for preventing denial-of-service scenarios.
    /// </para>
    /// <para>
    /// For simple calculator operations, the timeout is rarely triggered since
    /// arithmetic is nearly instantaneous. However, this infrastructure supports
    /// future extensions with more complex operations.
    /// </para>
    /// <para><b>Thread Safety:</b> Each <see cref="ExecutionContext"/> instance
    /// can safely execute operations concurrently.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a context with 5-second timeout
    /// var context = new ExecutionContext(TimeSpan.FromSeconds(5));
    ///
    /// // Execute an operation with timeout protection
    /// var result = context.Execute(() => {
    ///     return SomeExpensiveCalculation();
    /// }, "ExpensiveCalculation");
    /// </code>
    /// </example>
    public sealed class ExecutionContext
    {
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Creates an execution context with the specified timeout.
        /// </summary>
        /// <param name="timeout">Maximum time allowed for operation execution.</param>
        /// <exception cref="ArgumentException">Thrown when timeout is not positive.</exception>
        /// <example>
        /// <code>
        /// // 5-second timeout
        /// var context = new ExecutionContext(TimeSpan.FromSeconds(5));
        ///
        /// // 100ms timeout for quick operations
        /// var fastContext = new ExecutionContext(TimeSpan.FromMilliseconds(100));
        /// </code>
        /// </example>
        public ExecutionContext(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout must be positive", nameof(timeout));
            }

            _timeout = timeout;
        }

        /// <summary>
        /// Executes an operation with timeout enforcement.
        /// </summary>
        /// <typeparam name="T">Return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">Name of the operation for error messages.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="TimeoutException">Thrown when operation exceeds timeout.</exception>
        /// <remarks>
        /// <para>
        /// The operation runs on a thread pool thread. If it exceeds the timeout,
        /// a <see cref="TimeoutException"/> is thrown. Note that the operation may
        /// continue running in the background even after timeout.
        /// </para>
        /// <para>
        /// Any exception thrown by the operation is unwrapped from the
        /// <see cref="AggregateException"/> and re-thrown directly.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var context = new ExecutionContext(TimeSpan.FromSeconds(5));
        ///
        /// try
        /// {
        ///     var result = context.Execute(() => {
        ///         return ComputeValue();
        ///     }, "ComputeValue");
        /// }
        /// catch (TimeoutException)
        /// {
        ///     // Operation took too long
        /// }
        /// </code>
        /// </example>
        public T Execute<T>(Func<T> operation, string operationName)
        {
            using var cts = new CancellationTokenSource(_timeout);
            var task = Task.Run(operation, cts.Token);

            try
            {
                // Wait for completion or timeout
                if (!task.Wait(_timeout))
                {
                    throw new TimeoutException(
                        $"Operation '{operationName}' exceeded timeout of {_timeout.TotalMilliseconds}ms. " +
                        $"This limit prevents resource exhaustion and ensures responsiveness.");
                }

                return task.Result;
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                throw new TimeoutException(
                    $"Operation '{operationName}' was cancelled due to timeout ({_timeout.TotalMilliseconds}ms).",
                    ex.InnerException);
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                // Unwrap AggregateException to expose the actual exception
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Executes an operation with timeout enforcement (void return).
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">Name of the operation for error messages.</param>
        /// <exception cref="TimeoutException">Thrown when operation exceeds timeout.</exception>
        /// <example>
        /// <code>
        /// var context = new ExecutionContext(TimeSpan.FromSeconds(5));
        ///
        /// context.Execute(() => {
        ///     PerformSideEffect();
        /// }, "PerformSideEffect");
        /// </code>
        /// </example>
        public void Execute(Action operation, string operationName)
        {
            Execute(() =>
            {
                operation();
                return 0; // Dummy return for Func<int>
            }, operationName);
        }
    }
}
