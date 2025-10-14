# Docker Setup Guide

Complete guide for using Docker with the Game Server Hosting platform.

## 🐳 Overview

The project uses Docker Compose to run PostgreSQL and optional services for local development.

## 📋 Services

### PostgreSQL Database

**Container**: `gameserver-postgres`  
**Image**: `postgres:16-alpine`  
**Port**: `5432`  
**Database**: `gameserverhosting_dev`  
**Credentials**: `postgres/postgres`  

### pgAdmin (Optional)

**Container**: `gameserver-pgadmin`  
**Image**: `dpage/pgadmin4:latest`  
**Port**: `5050`  
**URL**: `http://localhost:5050`  
**Credentials**: `admin@gameserver.local / admin`  

## 🚀 Quick Start

```bash
# Start PostgreSQL only
docker-compose up -d postgres

# Start all services (PostgreSQL + pgAdmin)
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: deletes data)
docker-compose down -v
```

## 📝 Docker Compose File

The `docker-compose.yml` at the project root:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: gameserver-postgres
    restart: unless-stopped
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: gameserverhosting_dev
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: gameserver-pgadmin
    restart: unless-stopped
    ports:
      - "5050:80"
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@gameserver.local
      PGADMIN_DEFAULT_PASSWORD: admin
    volumes:
      - pgadmin_data:/var/lib/pgadmin
    depends_on:
      - postgres

volumes:
  postgres_data:
    driver: local
  pgadmin_data:
    driver: local
```

## 🔧 Common Commands

### Container Management

```bash
# Start services
docker-compose up -d                    # All services, detached mode
docker-compose up -d postgres           # PostgreSQL only
docker-compose up                       # All services, see logs

# Stop services
docker-compose stop                     # Stop without removing
docker-compose down                     # Stop and remove containers

# Restart services
docker-compose restart postgres         # Restart specific service
docker-compose restart                  # Restart all
```

### Monitoring

```bash
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a

# View logs
docker-compose logs                     # All services
docker-compose logs -f postgres         # Follow PostgreSQL logs
docker-compose logs --tail=50 postgres  # Last 50 lines

# Check resource usage
docker stats gameserver-postgres
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

You can customize via environment variables:

```bash
# Example: .env file
POSTGRES_USER=myuser
POSTGRES_PASSWORD=mypassword
POSTGRES_DB=mydatabase
POSTGRES_PORT=5432
```

Then update `docker-compose.yml`:
```yaml
environment:
  POSTGRES_USER: ${POSTGRES_USER}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  POSTGRES_DB: ${POSTGRES_DB}
```

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
docker logs gameserver-postgres
```

**Common issues:**
- Port 5432 already in use
- Insufficient memory
- Corrupted volume

**Solutions:**
```bash
# Remove and recreate
docker-compose down
docker rm gameserver-postgres
docker-compose up -d postgres

# Nuclear option (deletes data)
docker-compose down -v
docker-compose up -d postgres
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

## 🚀 Advanced Usage

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

## 💡 Tips

1. **Use named volumes** for data persistence
2. **Enable health checks** to know when container is ready
3. **Use specific image tags** (e.g., `postgres:16-alpine`) not `latest`
4. **Keep credentials in environment variables** for production
5. **Regular backups** of production data
6. **Monitor resource usage** with `docker stats`

---

Docker makes local development easy! 🐳

