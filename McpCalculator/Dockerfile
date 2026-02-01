# Multi-stage build for MCP Calculator with sandboxing
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY McpCalculator.csproj .
RUN dotnet restore

# Copy source and build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage - minimal image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN useradd -m -u 1000 mcpuser && \
    chown -R mcpuser:mcpuser /app

# Copy published application
COPY --from=build --chown=mcpuser:mcpuser /app/publish .

# Switch to non-root user
USER mcpuser

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV ASPNETCORE_URLS=

# Run the MCP server
ENTRYPOINT ["dotnet", "McpCalculator.dll"]

# Usage examples:
# Build: docker build -t mcpcalculator .
#
# Run with resource limits:
# docker run \
#   --memory="128m" \
#   --cpus="0.5" \
#   --network=none \
#   --read-only \
#   --tmpfs /tmp:size=10m \
#   --security-opt=no-new-privileges \
#   --cap-drop=ALL \
#   mcpcalculator
#
# For MCP stdio transport via Docker:
# docker run -i mcpcalculator
#
# Security features:
# - Non-root user (uid 1000)
# - Read-only filesystem
# - No network access
# - Memory limit: 128MB
# - CPU limit: 50% of one core
# - No new privileges
# - All capabilities dropped
