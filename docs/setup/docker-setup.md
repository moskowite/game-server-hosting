# Docker Setup Guide

Complete guide for using Docker with the Game Server Hosting platform.

## 🐳 Overview

The project uses Docker Compose to run the complete application stack, including:
- API (ASP.NET Core)
- Client (Blazor WebAssembly)
- PostgreSQL database
- MailDev (SMTP server for development)
- pgAdmin (database management UI)

All services are containerized with production-ready Dockerfiles and optimized for both development and production environments.

## 📋 Services

### API Service

**Container**: `gameserver-api`  
**Port**: `5292:8080`  
**URL**: `http://localhost:5292`  
**Swagger**: `http://localhost:5292/swagger`  
**Health Check**: `http://localhost:5292/health`  

Built using multi-stage Dockerfile:
- Stage 1: Build with .NET SDK 8.0
- Stage 2: Runtime with ASP.NET Core runtime
- Features: Optimized layer caching, non-root user, health checks

### Client Service

**Container**: `gameserver-client`  
**Port**: `5173:80`  
**URL**: `http://localhost:5173`  
**Health Check**: `http://localhost:5173/health`  

Built using multi-stage Dockerfile:
- Stage 1: Build Blazor WASM with .NET SDK
- Stage 2: Serve with nginx
- Features: Gzip compression, optimized caching, SPA routing

### PostgreSQL Database

**Container**: `gameserver-postgres`  
**Image**: `postgres:16-alpine`  
**Port**: `5432`  
**Database**: `gameserverhosting_dev`  
**Credentials**: `postgres/postgres`  

### MailDev (Development SMTP)

**Container**: `gameserver-maildev`  
**Image**: `maildev/maildev:latest`  
**Web UI**: `http://localhost:1080`  
**SMTP Port**: `1025`  

Perfect for testing email functionality without sending real emails.

### pgAdmin (Optional)

**Container**: `gameserver-pgadmin`  
**Image**: `dpage/pgadmin4:latest`  
**Port**: `5050`  
**URL**: `http://localhost:5050`  
**Credentials**: `admin@gameserver.local / admin`  

## 🚀 Quick Start

### One-Command Startup (Recommended)

Start the entire application stack with one command:

```bash
# Start all services (API, Client, Database, MailDev, pgAdmin)
docker compose up -d

# View logs
docker compose logs -f

# Stop all services
docker compose down
```

Access the application:
- **Client**: http://localhost:5173
- **API**: http://localhost:5292
- **Swagger**: http://localhost:5292/swagger
- **MailDev UI**: http://localhost:1080
- **pgAdmin**: http://localhost:5050

### Individual Services

```bash
# Start only the database
docker compose up -d postgres

# Start API (requires database)
docker compose up -d api

# Start Client (requires API)
docker compose up -d client

# Rebuild and start (after code changes)
docker compose up -d --build

# Stop all services
docker compose down

# Stop and remove volumes (WARNING: deletes data)
docker compose down -v
```

## 📝 Docker Files Structure

### Dockerfiles

**`src/GameServerHosting.Api/Dockerfile`**
- Multi-stage build (SDK → Runtime)
- Optimized layer caching
- Non-root user for security
- Health check endpoint
- Includes curl for health checks

**`src/GameServerHosting.Client/Dockerfile`**
- Multi-stage build (SDK → nginx)
- Optimized for static file serving
- Gzip compression enabled
- Custom nginx configuration
- SPA routing support

**`.dockerignore`**
- Excludes `bin/`, `obj/`, `node_modules/`
- Excludes documentation and test files
- Excludes `.git`, IDE files

### Docker Compose Files

**`docker-compose.yml`** - Production configuration
- Defines all services with production settings
- Named volumes for data persistence
- Custom network for service isolation
- Health checks for all services
- Proper service dependencies

**`docker-compose.override.yml`** - Development overrides
- Automatically merged in development
- Volume mounts for hot reload
- Debug ports exposed
- Development environment variables
- Detailed logging enabled

### Networks

All services run on a custom bridge network `gameserver-network` for:
- Service isolation
- DNS resolution by service name
- Enhanced security

## 🎯 Benefits

### Consistent Environments
- Identical setup across team members
- No "works on my machine" issues
- Same environment from development to production

### Simplified Setup
- One command to start entire stack
- No manual installation of dependencies
- Automatic service orchestration

### Production-Ready
- Container images ready for deployment
- Simplified CI/CD pipelines
- Easy scaling and management

## 🔧 Common Commands

### Container Management

```bash
# Start services
docker compose up -d                    # All services, detached mode
docker compose up -d postgres           # PostgreSQL only
docker compose up                       # All services, see logs
docker compose up -d --build            # Rebuild and start

# Stop services
docker compose stop                     # Stop without removing
docker compose down                     # Stop and remove containers

# Restart services
docker compose restart api              # Restart specific service
docker compose restart                  # Restart all

# Rebuild services (after code changes)
docker compose build                    # Rebuild all
docker compose build api                # Rebuild API only
```

### Monitoring

```bash
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a

# View logs
docker compose logs                     # All services
docker compose logs -f api              # Follow API logs
docker compose logs -f client           # Follow Client logs
docker compose logs --tail=50 postgres  # Last 50 lines
docker compose logs api client          # Multiple services

# Check resource usage
docker stats                            # All containers
docker stats gameserver-api             # Specific container

# Check health status
docker inspect gameserver-api --format='{{.State.Health.Status}}'
```

### Database Access

```bash
# Open psql shell
docker exec -it gameserver-postgres psql -U postgres -d gameserverhosting_dev

# Run SQL command
docker exec gameserver-postgres psql -U postgres -d gameserverhosting_dev -c "SELECT * FROM server_types;"

# Backup database
docker exec gameserver-postgres pg_dump -U postgres gameserverhosting_dev > backup.sql

# Restore database
docker exec -i gameserver-postgres psql -U postgres gameserverhosting_dev < backup.sql
```

## ⚙️ Configuration

### Environment Variables

Create a `.env` file in the project root to customize settings:

```bash
# Database Configuration
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=gameserverhosting_dev

# API Configuration
JWT_SECRET_KEY=your-super-secret-key-at-least-32-characters-long
ASPNETCORE_ENVIRONMENT=Development

# Stripe Configuration (optional)
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# DigitalOcean Configuration (optional)
DIGITALOCEAN_API_TOKEN=dop_v1_...
```

These variables are automatically loaded by Docker Compose.

### Development vs Production

**Development Mode** (default with `docker-compose.override.yml`):
- `ASPNETCORE_ENVIRONMENT=Development`
- Volume mounts for hot reload
- Detailed logging
- CORS allows all localhost origins
- Debug ports exposed

**Production Mode** (use `-f docker-compose.yml` only):
```bash
docker compose -f docker-compose.yml up -d
```
- `ASPNETCORE_ENVIRONMENT=Production`
- No volume mounts
- Optimized logging
- Strict CORS settings
- No debug ports

### Volumes

Data persists in named Docker volumes:

```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect game-server-hosting_postgres_data

# Remove volume (WARNING: deletes all data)
docker volume rm game-server-hosting_postgres_data
```

### Health Checks

PostgreSQL has a health check configured:

```bash
# Check health status
docker inspect --format='{{.State.Health.Status}}' gameserver-postgres
```

Status should be `healthy` before connecting.

## 🐛 Troubleshooting

### Container Won't Start

**Check logs:**
```bash
docker logs gameserver-api
docker logs gameserver-client
docker logs gameserver-postgres
```

**Common issues:**
- Port already in use
- Insufficient memory
- Build errors
- Missing environment variables
- Database not ready

**Solutions:**
```bash
# Rebuild containers
docker compose build --no-cache
docker compose up -d

# Check service health
docker compose ps

# Restart specific service
docker compose restart api

# Nuclear option (deletes data)
docker compose down -v
docker compose up -d --build
```

### API Can't Connect to Database

**Symptom**: API logs show connection errors

**Solution**: Ensure database is healthy before API starts
```bash
# Check database health
docker inspect gameserver-postgres --format='{{.State.Health.Status}}'

# Should show "healthy" - if not, check database logs
docker logs gameserver-postgres

# Restart in correct order
docker compose down
docker compose up -d postgres
# Wait for database to be healthy
docker compose up -d api
```

### Client Can't Connect to API

**Symptom**: Client shows API connection errors

**Check API is running:**
```bash
curl http://localhost:5292/health
```

**Verify environment:**
- Check `ApiSettings__BaseUrl` in client environment
- Ensure CORS is properly configured in API
- Check network connectivity between containers

### Build Errors

**Symptom**: Docker build fails

**Common causes:**
- Missing project files
- Incorrect Dockerfile paths
- .dockerignore excluding needed files

**Solution:**
```bash
# Clean build
docker compose build --no-cache api

# Check build context
docker compose build --progress=plain api

# Verify .dockerignore isn't excluding required files
cat .dockerignore
```

### Port Already in Use

**Error**: `port is already allocated`

**Solution:**
```bash
# Find what's using port 5432
netstat -ano | findstr :5432

# Kill the process
taskkill /PID <PID> /F

# Or change port in docker-compose.yml
ports:
  - "5433:5432"  # Use 5433 on host
```

### Can't Connect to Database

**Verify container is running:**
```bash
docker ps | grep gameserver-postgres
```

**Check container is healthy:**
```bash
docker inspect gameserver-postgres | findstr Status
```

**Test connection:**
```bash
docker exec gameserver-postgres psql -U postgres -c "SELECT 1;"
```

### Data Lost After Restart

**Cause**: Volume not properly configured

**Solution**: Verify volumes in `docker-compose.yml`:
```yaml
volumes:
  - postgres_data:/var/lib/postgresql/data  # Named volume (persists)
  # NOT: ./data:/var/lib/postgresql/data    # Local folder (may have issues)
```

## 🔐 Security Notes

### Development vs Production

**Development** (Current Setup):
- Simple credentials (`postgres/postgres`)
- No SSL
- Local network only
- pgAdmin exposed

**Production** (Required Changes):
- Strong passwords (environment variables)
- SSL enabled (`POSTGRES_SSL_MODE=require`)
- Firewall rules
- No pgAdmin or restricted access
- Regular backups

### Securing PostgreSQL

For production deployments:

```yaml
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}  # From environment
  POSTGRES_SSL_MODE: require
  POSTGRES_INITDB_ARGS: "--auth-host=scram-sha-256"
```

## 🔄 Development Workflow

### Hot Reload with Volume Mounts

The `docker-compose.override.yml` enables hot reload for development:

**API Hot Reload:**
- Source code is mounted into the container
- Changes require container restart to take effect
- Use `docker compose restart api` after code changes

**Client Development:**
- For active development, consider running client locally with `dotnet run`
- Or rebuild client container: `docker compose build client && docker compose up -d client`

### Watching for Changes

For a better development experience with auto-reload:

```bash
# Option 1: Run API locally, use Docker for services
docker compose up -d postgres maildev
dotnet run --project src/GameServerHosting.Api

# Option 2: Run Client locally
docker compose up -d postgres maildev api
dotnet run --project src/GameServerHosting.Client
```

### Testing Email Functionality

MailDev captures all emails sent by the API:

1. Start the stack: `docker compose up -d`
2. Trigger an email (e.g., user registration)
3. View emails at http://localhost:1080
4. No real emails are sent!

**MailDev Features:**
- Web UI to view all emails
- REST API for automated testing
- Email inspection and debugging
- No configuration needed

### Database Migrations

Run Entity Framework migrations in the containerized environment:

```bash
# Run migrations inside API container
docker compose exec api dotnet ef database update

# Or run migrations from host (requires connection to containerized DB)
dotnet ef database update --project src/GameServerHosting.Api

# Create new migration
dotnet ef migrations add MigrationName --project src/GameServerHosting.Api
```

## 🚀 Advanced Usage

### Production Deployment

Build production-ready images:

```bash
# Build with production settings
docker compose -f docker-compose.yml build

# Tag images for registry
docker tag gameserver-api:latest your-registry.com/gameserver-api:v1.0
docker tag gameserver-client:latest your-registry.com/gameserver-client:v1.0

# Push to registry
docker push your-registry.com/gameserver-api:v1.0
docker push your-registry.com/gameserver-client:v1.0
```

### Multiple Databases

To run separate databases for different purposes:

```yaml
services:
  postgres-dev:
    # Development database
    
  postgres-test:
    # Test database
    ports:
      - "5433:5432"  # Different port
```

### Custom Initialization

Create `init.sql` in project root:

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- For fuzzy search
-- Add more initialization SQL
```

Update `docker-compose.yml`:
```yaml
volumes:
  - ./init.sql:/docker-entrypoint-initdb.d/init.sql
  - postgres_data:/var/lib/postgresql/data
```

### Performance Tuning

For better performance in development:

```yaml
environment:
  # Increase shared buffers
  POSTGRES_SHARED_BUFFERS: 256MB
  # Increase work memory
  POSTGRES_WORK_MEM: 16MB
command:
  - "postgres"
  - "-c"
  - "shared_buffers=256MB"
  - "-c"
  - "work_mem=16MB"
```

## 📚 Related Documentation

- [Database Setup](database-setup.md) - Entity Framework Core and migrations
- [Quick Start](quick-start.md) - Initial setup
- [Local Development](local-development.md) - Complete development guide
- [Troubleshooting](../reference/troubleshooting.md) - Common issues

## 🎓 Best Practices

### Development
1. **Use `docker compose up -d`** for one-command startup
2. **Check health status** before debugging connection issues
3. **Use MailDev** for email testing instead of real SMTP
4. **Monitor logs** with `docker compose logs -f`
5. **Rebuild after dependency changes** with `--build` flag

### Production
1. **Use environment variables** for all secrets
2. **Use specific image tags** (not `latest`)
3. **Enable health checks** for orchestration
4. **Set resource limits** to prevent resource exhaustion
5. **Regular backups** of volumes
6. **Use `.env` files** for environment-specific configuration

### Security
1. **Never commit secrets** to version control
2. **Use strong passwords** in production
3. **Run containers as non-root** (already configured)
4. **Keep images updated** for security patches
5. **Use TLS/SSL** in production
6. **Restrict network access** appropriately

## 📊 Service Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Docker Network                      │
│              (gameserver-network)                    │
│                                                      │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐     │
│  │  Client  │───▶│   API    │───▶│ Postgres │     │
│  │  :5173   │    │  :5292   │    │  :5432   │     │
│  └──────────┘    └──────────┘    └──────────┘     │
│                        │                             │
│                        ▼                             │
│                   ┌──────────┐                      │
│                   │ MailDev  │                      │
│                   │  :1080   │                      │
│                   └──────────┘                      │
│                                                      │
└─────────────────────────────────────────────────────┘
```

## 💡 Quick Reference

### Essential Commands
```bash
# Start everything
docker compose up -d

# Stop everything
docker compose down

# Rebuild after code changes
docker compose up -d --build

# View logs
docker compose logs -f

# Check status
docker compose ps

# Execute command in container
docker compose exec api bash

# Database shell
docker compose exec postgres psql -U postgres -d gameserverhosting_dev
```

### Common URLs
- **Client**: http://localhost:5173
- **API**: http://localhost:5292
- **Swagger**: http://localhost:5292/swagger
- **MailDev**: http://localhost:1080
- **pgAdmin**: http://localhost:5050

---

Docker makes local development easy and production deployment consistent! 🐳

