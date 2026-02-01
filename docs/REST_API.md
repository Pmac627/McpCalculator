# REST API Guide

This document describes the REST API for the MCP Calculator web server. The REST API provides a traditional HTTP interface alongside the MCP protocol, allowing any HTTP client to perform calculations.

## Overview

The REST API complements the MCP HTTP/SSE transport by providing simple JSON endpoints for calculator operations. While MCP is designed for AI assistant integration, the REST API is ideal for:

- Web applications
- Mobile apps
- Scripts and automation
- Testing and debugging
- Clients that don't support MCP

## Base URL

```
https://your-server/api/calculator
```

## Authentication

The REST API uses the same authentication as the MCP endpoint. See [AUTHENTICATION.md](AUTHENTICATION.md) for details.

**Quick Reference:**

| Type | Header | Example |
|------|--------|---------|
| None | - | No authentication required |
| ApiKey | `X-API-Key: <key>` | `X-API-Key: your-api-key` |
| JWT | `Authorization: Bearer <token>` | `Authorization: Bearer eyJ...` |
| Windows | Automatic | NTLM/Kerberos negotiation |

---

## Endpoints

### POST /api/calculator/add

Add two numbers together.

**Request:**
```json
{
  "a": 10.5,
  "b": 3.2
}
```

**Response (200 OK):**
```json
{
  "result": 13.7,
  "operation": "Add"
}
```

**Example (curl):**
```bash
curl -X POST https://localhost:5001/api/calculator/add \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"a": 10.5, "b": 3.2}'
```

**Example (PowerShell):**
```powershell
$body = @{ a = 10.5; b = 3.2 } | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/calculator/add" `
  -Method POST `
  -ContentType "application/json" `
  -Headers @{ "X-API-Key" = "your-api-key" } `
  -Body $body
```

---

### POST /api/calculator/subtract

Subtract the second number from the first (a - b).

**Request:**
```json
{
  "a": 100,
  "b": 37
}
```

**Response (200 OK):**
```json
{
  "result": 63,
  "operation": "Subtract"
}
```

**Example (curl):**
```bash
curl -X POST https://localhost:5001/api/calculator/subtract \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"a": 100, "b": 37}'
```

---

### POST /api/calculator/multiply

Multiply two numbers together.

**Request:**
```json
{
  "a": 6,
  "b": 7
}
```

**Response (200 OK):**
```json
{
  "result": 42,
  "operation": "Multiply"
}
```

**Example (curl):**
```bash
curl -X POST https://localhost:5001/api/calculator/multiply \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"a": 6, "b": 7}'
```

---

### POST /api/calculator/divide

Divide the first number by the second (a / b).

**Request:**
```json
{
  "a": 100,
  "b": 4
}
```

**Response (200 OK):**
```json
{
  "result": 25,
  "operation": "Divide"
}
```

**Example (curl):**
```bash
curl -X POST https://localhost:5001/api/calculator/divide \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"a": 100, "b": 4}'
```

---

## Constraints

All operations enforce the following constraints:

| Constraint | Value | Description |
|------------|-------|-------------|
| Max Absolute Value | ±1e15 | Inputs and results must be within this range |
| Min Division Denominator | 1e-10 | Division denominator must have absolute value ≥ this |
| Rate Limit | 100/minute | Per operation (add, subtract, multiply, divide each have separate limits) |

---

## Error Responses

### 400 Bad Request - Validation Error

Returned when input values are invalid.

**Causes:**
- Value is NaN (Not a Number)
- Value is Infinity
- Value exceeds ±1e15
- Division by zero
- Division by near-zero (|b| < 1e-10)

**Response:**
```json
{
  "error": "ValidationError",
  "message": "Value exceeds maximum allowed: 1E+15",
  "operation": "Add"
}
```

**Example - Division by Zero:**
```json
{
  "error": "ValidationError",
  "message": "Cannot divide by zero (Parameter 'b')",
  "operation": "Divide"
}
```

---

### 400 Bad Request - Overflow Error

Returned when the calculation result exceeds allowed limits.

**Response:**
```json
{
  "error": "OverflowError",
  "message": "Result of Multiply exceeds maximum allowed value: 1E+15",
  "operation": "Multiply"
}
```

---

### 401 Unauthorized

Returned when authentication is required but not provided or invalid.

**Response:**
```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: ApiKey realm="McpCalculator", header="X-API-Key"
```

---

### 429 Too Many Requests

Returned when the rate limit is exceeded (100 requests per minute per operation).

**Response:**
```
HTTP/1.1 429 Too Many Requests
```

**Note:** Each operation has its own independent rate limit. You can make 100 add requests AND 100 multiply requests per minute.

---

### 500 Internal Server Error

Returned for unexpected server errors.

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Error details here"
}
```

---

## Request/Response Models

### CalculatorRequest

```csharp
public record CalculatorRequest(double A, double B);
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| a | number (double) | Yes | First operand |
| b | number (double) | Yes | Second operand |

### CalculatorResponse

```csharp
public record CalculatorResponse(double Result, string Operation);
```

| Field | Type | Description |
|-------|------|-------------|
| result | number (double) | The calculation result |
| operation | string | Name of the operation performed |

### CalculatorErrorResponse

```csharp
public record CalculatorErrorResponse(string Error, string Message, string Operation);
```

| Field | Type | Description |
|-------|------|-------------|
| error | string | Error type (ValidationError, OverflowError, etc.) |
| message | string | Human-readable error description |
| operation | string | Name of the operation that failed |

---

## Comparison: REST API vs MCP

| Feature | REST API | MCP (HTTP/SSE) |
|---------|----------|----------------|
| Protocol | HTTP POST | JSON-RPC 2.0 over HTTP/SSE |
| Use Case | Simple integrations | AI assistant integration |
| Request Format | Simple JSON | JSON-RPC envelope |
| Streaming | No | Yes (SSE) |
| Tool Discovery | No (documented) | Yes (`tools/list`) |
| Complexity | Low | Medium |

**REST API Request:**
```json
{
  "a": 5,
  "b": 3
}
```

**Equivalent MCP Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "Add",
    "arguments": { "a": 5, "b": 3 }
  }
}
```

---

## Client Examples

### JavaScript (fetch)

```javascript
async function add(a, b) {
  const response = await fetch('https://localhost:5001/api/calculator/add', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': 'your-api-key'
    },
    body: JSON.stringify({ a, b })
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  return response.json();
}

// Usage
const result = await add(5, 3);
console.log(result.result); // 8
```

### Python (requests)

```python
import requests

def add(a: float, b: float) -> float:
    response = requests.post(
        'https://localhost:5001/api/calculator/add',
        json={'a': a, 'b': b},
        headers={'X-API-Key': 'your-api-key'}
    )
    response.raise_for_status()
    return response.json()['result']

# Usage
result = add(5, 3)
print(result)  # 8.0
```

### C# (HttpClient)

```csharp
using System.Net.Http.Json;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");

var response = await client.PostAsJsonAsync(
    "https://localhost:5001/api/calculator/add",
    new { a = 5.0, b = 3.0 });

var result = await response.Content.ReadFromJsonAsync<CalculatorResponse>();
Console.WriteLine(result.Result); // 8

record CalculatorResponse(double Result, string Operation);
```

---

## Testing with Development Server

When running locally with authentication disabled:

```bash
# Start the server
dotnet run --project McpCalculator.Web

# Test (no API key needed in Development)
curl -X POST http://localhost:5000/api/calculator/add \
  -H "Content-Type: application/json" \
  -d '{"a": 5, "b": 3}'
```

---

## See Also

- [HTTP_TRANSPORT.md](HTTP_TRANSPORT.md) - MCP over HTTP/SSE
- [AUTHENTICATION.md](AUTHENTICATION.md) - Authentication options
- [IIS_DEPLOYMENT.md](IIS_DEPLOYMENT.md) - Production deployment
