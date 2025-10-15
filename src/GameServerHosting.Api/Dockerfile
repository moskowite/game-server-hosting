# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files for layer caching
COPY GameServerHosting.sln ./
COPY src/GameServerHosting.Api/GameServerHosting.Api.csproj src/GameServerHosting.Api/
COPY src/GameServerHosting.Client/GameServerHosting.Client.csproj src/GameServerHosting.Client/
COPY src/GameServerHosting.Shared/GameServerHosting.Shared.csproj src/GameServerHosting.Shared/

# Restore dependencies
RUN dotnet restore src/GameServerHosting.Api/GameServerHosting.Api.csproj

# Copy the rest of the source code
COPY . .

# Build the API project
WORKDIR /src/src/GameServerHosting.Api
RUN dotnet build GameServerHosting.Api.csproj -c Release -o /app/build

# Publish the API project
RUN dotnet publish GameServerHosting.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create directories for logs with proper permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs

# Copy published output from build stage
COPY --from=build /app/publish .

# Change ownership of the app directory
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose the port the app runs on
EXPOSE 8080

# Configure health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "GameServerHosting.Api.dll"]
