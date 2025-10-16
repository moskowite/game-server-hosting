# Docker Quick Start Guide

## 🚀 Get Started in 30 Seconds

```bash
# Clone the repository (if needed)
git clone <repository-url>
cd game-server-hosting

# Start the entire stack
docker compose up -d

# View logs (optional)
docker compose logs -f
```

**That's it!** The entire application is now running.

## 🌐 Access the Application

| Service | URL | Purpose |
|---------|-----|---------|
| **Client** | http://localhost:5173 | Main web application |
| **API** | http://localhost:5292 | Backend API |
| **Swagger** | http://localhost:5292/swagger | API documentation |
| **MailDev** | http://localhost:1080 | Email testing UI |
| **pgAdmin** | http://localhost:5050 | Database management |

## 📋 Essential Commands

```bash
# Start all services
docker compose up -d

# Stop all services
docker compose down

# View logs
docker compose logs -f                # All services
docker compose logs -f api            # Just API
docker compose logs -f client         # Just Client

# Rebuild after code changes
docker compose up -d --build

# Check service status
docker compose ps

# Restart a specific service
docker compose restart api
```

## ⚙️ Configuration (Optional)

For Stripe payments and server provisioning:

```bash
# 1. Copy the example environment file
cp .env.example .env

# 2. Edit .env and add your keys
nano .env  # or use your favorite editor

# 3. Restart the services
docker compose down
docker compose up -d
```

## 🛠️ Common Tasks

### View Database
1. Open http://localhost:5050 (pgAdmin)
2. Login: `admin@gameserver.local` / `admin`
3. Add server:
   - Host: `postgres`
   - Port: `5432`
   - Database: `gameserverhosting_dev`
   - Username: `postgres`
   - Password: `postgres`

### Test Email Functionality
1. Register a new user or trigger email
2. Open http://localhost:1080 (MailDev)
3. View captured emails (no real emails sent!)

### Check Service Health
```bash
# Check if services are healthy
docker compose ps

# Check API health endpoint
curl http://localhost:5292/health

# View container logs for debugging
docker compose logs api
```

## 🐛 Troubleshooting

### Services won't start
```bash
# Check what's wrong
docker compose ps
docker compose logs

# Rebuild from scratch
docker compose down -v
docker compose up -d --build
```

### Port already in use
```bash
# Find what's using the port
# Linux/Mac:
sudo lsof -i :5292

# Windows:
netstat -ano | findstr :5292

# Kill the process or change the port in docker-compose.yml
```

### Can't connect to API from Client
1. Ensure API is running: `docker compose ps`
2. Check API health: `curl http://localhost:5292/health`
3. View API logs: `docker compose logs api`

## 📖 More Information

- **Full Documentation**: [docs/setup/docker-setup.md](docs/setup/docker-setup.md)
- **Architecture**: [docs/development/architecture.md](docs/development/architecture.md)
- **Troubleshooting**: [docs/reference/troubleshooting.md](docs/reference/troubleshooting.md)

## 🎯 What's Running?

```
┌─────────────┐
│   Client    │  Blazor WASM (nginx)
│  :5173      │  
└──────┬──────┘
       │
       ▼
┌─────────────┐
│     API     │  ASP.NET Core 8.0
│  :5292      │  
└──────┬──────┘
       │
       ├──────► PostgreSQL (:5432)
       │
       └──────► MailDev (:1080, :1025)
```

---

**Need help?** Check the [full documentation](docs/setup/docker-setup.md) or [troubleshooting guide](docs/reference/troubleshooting.md).
