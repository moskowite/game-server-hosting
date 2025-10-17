# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Accept build configuration as argument (defaults to Release for production)
ARG BUILD_CONFIGURATION=Development
ENV BUILD_CONFIGURATION=${BUILD_CONFIGURATION}

# Copy solution file and project files for layer caching
COPY GameServerHosting.sln ./
COPY src/GameServerHosting.Api/GameServerHosting.Api.csproj src/GameServerHosting.Api/
COPY src/GameServerHosting.Client/GameServerHosting.Client.csproj src/GameServerHosting.Client/
COPY src/GameServerHosting.Domain/GameServerHosting.Domain.csproj src/GameServerHosting.Domain/
COPY src/GameServerHosting.Application/GameServerHosting.Application.csproj src/GameServerHosting.Application/
COPY src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj src/GameServerHosting.Infrastructure/
COPY src/GameServerHosting.Shared/GameServerHosting.Shared.csproj src/GameServerHosting.Shared/

# Restore dependencies
RUN dotnet restore src/GameServerHosting.Api/GameServerHosting.Api.csproj

# Copy the rest of the source code
COPY . .

# Build the API project with dynamic configuration
WORKDIR /src/src/GameServerHosting.Api
RUN dotnet build GameServerHosting.Api.csproj -c ${BUILD_CONFIGURATION} -o /app/build

# Publish the API project with dynamic configuration
RUN dotnet publish GameServerHosting.Api.csproj -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# Re-declare ARG after FROM to make it available in this stage
ARG BUILD_CONFIGURATION=Release

# Update packages and install curl for healthcheck
# This helps mitigate known vulnerabilities in base image
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

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
# Set ASPNETCORE_ENVIRONMENT based on build configuration
ENV ASPNETCORE_ENVIRONMENT=${BUILD_CONFIGURATION}

# Run the application
ENTRYPOINT ["dotnet", "GameServerHosting.Api.dll"]
