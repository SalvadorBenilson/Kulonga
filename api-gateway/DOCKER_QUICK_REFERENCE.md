# Docker Setup: API Gateway + SQL Server

## Overview

This setup provides a complete Docker environment with:
- **SQL Server 2022 Express** for database
- **API Gateway** (.NET 9.0 ASP.NET Core) 
- **Auto-migrations** on startup
- **Health checks** for service reliability
- **Network isolation** between services

## Quick Start (3 steps)

### 1. Prerequisites

- Docker Desktop installed ([Download](https://www.docker.com/products/docker-desktop/))
- Docker daemon running
- At least 4GB available RAM

### 2. Start Services

```bash
# Navigate to project directory
cd /Users/benilsonfernandosalvador/Desktop/Aegis/Kulonga/api-gateway

# Option A: Using quick start script (recommended)
bash quickstart.sh
# Select option "1" for production mode

# Option B: Direct command
docker compose up --build -d
```

### 3. Verify

```bash
# Check services
docker compose ps

# Test API
curl http://localhost:5230/schools

# View logs
docker compose logs -f
```

## Services

### SQL Server
```
Container: sqlserver-db
Port: 1433 (on host: localhost:1433)
Username: sa
Password: YourPassword123!
Database: ApiGatewayDb (auto-created)
```

### API Gateway
```
Container: api-gateway
Port: 5230 (on host: http://localhost:5230)
Environment: Production
Auto-runs: Database migrations on startup
```

## Common Commands

### Start/Stop
```bash
# Start
docker compose up -d

# Stop
docker compose down

# View status
docker compose ps

# Restart
docker compose restart
```

### Logs
```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f sqlserver
docker compose logs -f apigateway

# Last 50 lines
docker compose logs --tail 50
```

### Database Access

**From Host Machine (SQL Server Management Studio):**
```
Server: localhost,1433
Authentication: SQL Server
Username: sa
Password: YourPassword123!
```

**From Command Line:**
```bash
# Execute SQL
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourPassword123!" \
  -Q "SELECT name FROM sys.databases"
```

### API Testing

```bash
# Health check
curl http://localhost:5230/schools

# Get all schools
curl http://localhost:5230/schools

# Create school
curl -X POST http://localhost:5230/schools \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Lincoln High School",
    "code": "LHS",
    "subdomain": "lincoln",
    "email": "admin@lincolnhigh.edu"
  }'
```

## Debug Mode

For development with Visual Studio Code debugging:

```bash
# Using debug compose (includes debug configuration)
docker compose -f compose.debug.yaml up --build -d

# View debug logs
docker compose -f compose.debug.yaml logs -f
```

Debug compose includes:
- Development environment
- Visual Studio debug support via remote debugger
- Separate network and volumes

## Configuration

### Environment Variables

Edit `docker-compose.yaml` to configure:

```yaml
environment:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: "Server=sqlserver,..."
  Jwt__Secret: "your-secret-key"
```

### Using .env File

1. Copy `.env.example` to `.env`
2. Edit `.env` with your values
3. Docker Compose automatically loads it

```bash
cp .env.example .env
# Edit .env with your settings
docker compose up --build -d
```

## Troubleshooting

### Services not starting
```bash
# Check individual service logs
docker compose logs sqlserver
docker compose logs apigateway

# Rebuild without cache
docker compose build --no-cache
docker compose up -d
```

### "Connection refused" error
- SQL Server may still be starting (takes ~30 seconds)
- Check health: `docker compose ps`
- Wait for "healthy" status before API starts

### Port already in use
```yaml
# In compose.yaml, change port:
ports:
  - "1434:1433"  # Use 1434 instead of 1433

# Update connection string
ConnectionStrings__DefaultConnection: "Server=localhost,1434;..."
```

### Database migration errors
```bash
# Check API logs for migration status
docker compose logs apigateway | grep -i migration

# Reset database
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourPassword123!" \
  -Q "DROP DATABASE IF EXISTS ApiGatewayDb"

# Restart API to recreate
docker compose restart apigateway
```

## Performance

### For Development
- Use `compose.debug.yaml`
- SQL Server Express (10GB limit)
- Single API instance

### For Production
- Update `compose.yaml`:
  ```yaml
  MSSQL_PID: Standard  # or Enterprise
  deploy:
    replicas: 2  # Multiple instances
  ```
- Configure proper resource limits
- Use volume backups
- Enable monitoring

## Files Structure

```
api-gateway/
├── compose.yaml              # Production compose
├── compose.debug.yaml        # Debug compose
├── .env.example              # Configuration template
├── quickstart.sh             # Quick start script
├── start.sh                  # Startup helper
├── api-gateway/
│   ├── Dockerfile            # Enhanced with health checks
│   ├── api-gateway.csproj
│   ├── Program.cs            # Auto-migrations on startup
│   ├── appsettings.json      # Updated with docker connection string
│   └── ...
├── DOCKER_SQLSERVER_SETUP.md # Detailed guide
└── DOCKER_QUICK_REFERENCE.md # This file
```

## What Happens on Startup

1. **Docker Compose starts**
   - Creates network `kulonga-network`
   - Starts SQL Server container

2. **SQL Server initializes** (~30s)
   - Runs health check every 10 seconds
   - Waits for database to accept connections

3. **API Gateway starts** (after SQL Server healthy)
   - Connects to SQL Server
   - Runs EF Core migrations
     - Creates database if needed
     - Applies all pending migrations
     - Seeds initial data (if configured)
   - Listens on port 5230

4. **Services ready**
   - Both containers running and healthy
   - API Gateway accepts requests
   - Database ready for operations

## Cleanup

```bash
# Stop and remove containers
docker compose down

# Also remove volumes (delete data)
docker compose down -v

# Remove images
docker rmi apigateway mssql/server:2022-latest

# Complete cleanup
docker compose down --rmi all -v
docker network prune
```

## Next Steps

1. ✅ Start services: `docker compose up -d`
2. 📊 Check database with SQL Server Management Studio
3. 🧪 Test API endpoints
4. 📝 Modify `appsettings.json` as needed
5. 🔧 Configure OAuth (Google/Facebook credentials)
6. 🚀 Deploy to production

## Support

For detailed information, see:
- [DOCKER_SQLSERVER_SETUP.md](DOCKER_SQLSERVER_SETUP.md) - Comprehensive guide
- [SUBDOMAIN_CONFIGURATION.md](SUBDOMAIN_CONFIGURATION.md) - Subdomain routing
- [CONFIGURATION_SUMMARY.md](CONFIGURATION_SUMMARY.md) - Complete configuration

## References

- [SQL Server on Linux/Docker](https://hub.docker.com/_/microsoft-windows-servercore-powershell)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [ASP.NET Core Docker](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)
