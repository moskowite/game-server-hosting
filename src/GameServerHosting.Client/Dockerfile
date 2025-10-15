# Stage 1: Build Blazor WASM
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files for layer caching
COPY GameServerHosting.sln ./
COPY src/GameServerHosting.Api/GameServerHosting.Api.csproj src/GameServerHosting.Api/
COPY src/GameServerHosting.Client/GameServerHosting.Client.csproj src/GameServerHosting.Client/
COPY src/GameServerHosting.Shared/GameServerHosting.Shared.csproj src/GameServerHosting.Shared/

# Restore dependencies
RUN dotnet restore src/GameServerHosting.Client/GameServerHosting.Client.csproj

# Copy the rest of the source code
COPY . .

# Build and publish the Client project
WORKDIR /src/src/GameServerHosting.Client
RUN dotnet publish GameServerHosting.Client.csproj -c Release -o /app/publish

# Stage 2: Serve with nginx
FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# Copy custom nginx configuration
COPY --from=build /src/src/GameServerHosting.Client/nginx.conf /etc/nginx/nginx.conf

# Copy published Blazor WASM files
COPY --from=build /app/publish/wwwroot .

# Enable gzip compression for better performance
RUN apk add --no-cache gzip && \
    find . -type f \( -name '*.js' -o -name '*.css' -o -name '*.json' -o -name '*.html' \) \
    -exec gzip -k {} \;

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost/ || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
