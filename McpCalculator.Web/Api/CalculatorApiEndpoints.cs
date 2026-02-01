using McpCalculator.Core;
using McpCalculator.Web.Api.Models;

namespace McpCalculator.Web.Api
{
    /// <summary>
    /// Extension methods for mapping REST API calculator endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a clean way to register REST API endpoints for calculator operations.
    /// The endpoints complement the MCP transport, offering a traditional REST interface for
    /// clients that don't support the MCP protocol.
    /// </para>
    /// <para><b>API Design:</b></para>
    /// <list type="bullet">
    ///   <item>All endpoints use POST method with JSON body</item>
    ///   <item>Request body: <c>{"a": number, "b": number}</c></item>
    ///   <item>Success response: <c>{"result": number, "operation": string}</c></item>
    ///   <item>Error response: <c>{"error": string, "message": string, "operation": string}</c></item>
    /// </list>
    /// <para><b>Authentication:</b></para>
    /// <para>
    /// All endpoints require authorization (unless authentication type is "None").
    /// The same authentication configuration applies to both MCP and REST endpoints.
    /// </para>
    /// </remarks>
    /// <example>
    /// Register endpoints in Program.cs:
    /// <code>
    /// app.MapCalculatorApi();
    /// </code>
    /// </example>
    public static class CalculatorApiEndpoints
    {
        /// <summary>
        /// Maps all calculator REST API endpoints under /api/calculator.
        /// </summary>
        /// <param name="app">The web application to add endpoints to.</param>
        /// <returns>A route group builder for further configuration.</returns>
        /// <remarks>
        /// <para><b>Endpoints registered:</b></para>
        /// <list type="bullet">
        ///   <item><c>POST /api/calculator/add</c> - Add two numbers</item>
        ///   <item><c>POST /api/calculator/subtract</c> - Subtract second from first</item>
        ///   <item><c>POST /api/calculator/multiply</c> - Multiply two numbers</item>
        ///   <item><c>POST /api/calculator/divide</c> - Divide first by second</item>
        /// </list>
        /// </remarks>
        public static RouteGroupBuilder MapCalculatorApi(this WebApplication app)
        {
            // Create a single instance of CalculatorTools for all API calls
            // The tools class is stateless (except for the static rate limiter)
            var calculator = new CalculatorTools();

            // Group all calculator endpoints under /api/calculator
            var group = app.MapGroup("/api/calculator")
                .WithTags("Calculator");

            // POST /api/calculator/add
            group.MapPost("/add", (CalculatorRequest request) =>
                ExecuteOperation(request, "Add", () => calculator.Add(request.A, request.B)))
                .WithName("Add")
                .WithSummary("Add two numbers")
                .WithDescription("Returns the sum of two numbers. Both operands must be within ±1e15.");

            // POST /api/calculator/subtract
            group.MapPost("/subtract", (CalculatorRequest request) =>
                ExecuteOperation(request, "Subtract", () => calculator.Subtract(request.A, request.B)))
                .WithName("Subtract")
                .WithSummary("Subtract two numbers")
                .WithDescription("Returns the difference of two numbers (a - b). Both operands must be within ±1e15.");

            // POST /api/calculator/multiply
            group.MapPost("/multiply", (CalculatorRequest request) =>
                ExecuteOperation(request, "Multiply", () => calculator.Multiply(request.A, request.B)))
                .WithName("Multiply")
                .WithSummary("Multiply two numbers")
                .WithDescription("Returns the product of two numbers. Both operands must be within ±1e15.");

            // POST /api/calculator/divide
            group.MapPost("/divide", (CalculatorRequest request) =>
                ExecuteOperation(request, "Divide", () => calculator.Divide(request.A, request.B)))
                .WithName("Divide")
                .WithSummary("Divide two numbers")
                .WithDescription("Returns the quotient of two numbers (a / b). Denominator cannot be zero or have absolute value less than 1e-10.");

            return group;
        }

        /// <summary>
        /// Executes a calculator operation with proper error handling.
        /// </summary>
        /// <param name="request">The calculator request containing operands.</param>
        /// <param name="operationName">Name of the operation for error reporting.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>An IResult representing success or failure.</returns>
        /// <remarks>
        /// <para>
        /// This method wraps all calculator operations with consistent error handling,
        /// converting exceptions to appropriate HTTP status codes:
        /// </para>
        /// <list type="bullet">
        ///   <item><b>200 OK:</b> Successful calculation</item>
        ///   <item><b>400 Bad Request:</b> Validation errors (invalid input, division by zero)</item>
        ///   <item><b>429 Too Many Requests:</b> Rate limit exceeded</item>
        ///   <item><b>500 Internal Server Error:</b> Unexpected errors</item>
        /// </list>
        /// </remarks>
        internal static IResult ExecuteOperation(
            CalculatorRequest request,
            string operationName,
            Func<double> operation)
        {
            try
            {
                var result = operation();
                return Results.Ok(new CalculatorResponse(result, operationName));
            }
            catch (ArgumentException ex)
            {
                // Validation errors: NaN, Infinity, out of range, division by zero
                // Note: ArgumentOutOfRangeException inherits from ArgumentException,
                // so this catches both types with the same handling
                return Results.BadRequest(new CalculatorErrorResponse(
                    "ValidationError",
                    ex.Message,
                    operationName));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
            {
                // Rate limit exceeded - use 429 Too Many Requests
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            }
            catch (InvalidOperationException ex)
            {
                // Result overflow or other operation errors
                return Results.BadRequest(new CalculatorErrorResponse(
                    "OverflowError",
                    ex.Message,
                    operationName));
            }
            catch (Exception ex)
            {
                // Unexpected errors - log and return 500
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error");
            }
        }
    }
}
