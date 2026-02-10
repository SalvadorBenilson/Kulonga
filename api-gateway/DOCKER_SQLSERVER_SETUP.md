# Running API Gateway with SQL Server in Docker

## Quick Start

```bash
# Build and run everything
docker compose up --build

# Run in detached mode
docker compose up -d --build

# View logs
docker compose logs -f

# Stop
docker compose down
```

## What's Included

### Services

1. **SQL Server 2022 Express**
   - Image: `mcr.microsoft.com/mssql/server:2022-latest`
   - Port: `1433`
   - Username: `sa`
   - Password: `YourPassword123!`
   - Database: `ApiGatewayDb`

2. **API Gateway**
   - .NET 9.0 ASP.NET Core
   - Port: `5230`
   - Wait for SQL Server (healthcheck dependency)
   - Auto-runs migrations on startup

## Architecture

```
┌─────────────────────────────────────────┐
│         Docker Network                  │
│         kulonga-network                 │
│                                         │
│  ┌──────────────┐   ┌──────────────┐   │
│  │  SQL Server  │   │ API Gateway  │   │
│  │  (1433)      │   │  (5230)      │   │
│  └──────────────┘   └──────────────┘   │
│        │                   │            │
│        └───────────────────┘            │
│        (Docker Network)                 │
│                                         │
└─────────────────────────────────────────┘
         │
    ┌────┴─────┐
    │           │
Host:1433  Host:5230
```

## Connection Flow

1. **Startup**: `docker compose up --build`
2. **SQL Server** starts and waits for health check
3. **Health Check** verifies SQL Server is accepting connections on port 1433
4. **API Gateway** depends on SQL Server health check
5. **API Gateway** starts and automatically runs migrations
6. **Ready**: Both services are running and connected

## Connection Strings

### In Docker (Container to Container)
```
Server=sqlserver,1433;Database=ApiGatewayDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;
```

### From Host Machine
```
Server=localhost,1433;Database=ApiGatewayDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;
```

## Common Commands

### View Running Containers
```bash
docker compose ps
```

### View Logs
```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f sqlserver
docker compose logs -f apigateway
```

### Connect to SQL Server from Host

**Using SQL Server Management Studio:**
- Server: `localhost,1433`
- Authentication: SQL Server
- Username: `sa`
- Password: `YourPassword123!`

**Using command line:**
```bash
# Using sqlcmd (requires SQL Server tools)
sqlcmd -S localhost,1433 -U sa -P "YourPassword123!"

# Using docker exec
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!"
```

### Connect to API Gateway

```bash
# Health check
curl http://localhost:5230/health

# Example endpoints
curl http://localhost:5230/schools
curl http://localhost:5230/validate-token -X POST
```

### Execute SQL in Container

```bash
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "SELECT name FROM sys.databases"
```

## Database Operations

### Create Initial Database

The API Gateway will automatically create the database and run migrations on startup. You can verify:

```bash
# Check if database exists
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "SELECT name FROM sys.databases WHERE name = 'ApiGatewayDb'"
```

### Reset Database (if needed)

```bash
# Drop and recreate
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "DROP DATABASE IF EXISTS ApiGatewayDb"

# Restart API Gateway to recreate
docker compose restart apigateway
```

### Backup Database

```bash
docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "BACKUP DATABASE ApiGatewayDb TO DISK = '/var/opt/mssql/ApiGatewayDb.bak'"
```

## Environment Variables

### API Gateway Environment

Override default values by setting environment variables:

```bash
# In docker-compose.yaml
environment:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;..."
  Jwt__Secret: "your-custom-secret"
  Jwt__Issuer: "api-gateway"
```

### SQL Server Environment

```bash
# In docker-compose.yaml
environment:
  ACCEPT_EULA: "Y"
  SA_PASSWORD: "YourPassword123!"
  MSSQL_PID: Express
```

## Troubleshooting

### Issue: Connection timeout

**Solution:**
1. Check SQL Server is running: `docker compose ps`
2. Verify healthcheck: `docker compose logs sqlserver`
3. Wait a bit more (initial startup takes ~30 seconds): `sleep 30 && docker compose logs apigateway`

### Issue: "Cannot connect to sqlserver"

**Solutions:**
- Ensure both containers are on the same network: `docker network ls` and `docker network inspect kulonga-network`
- Verify service name is `sqlserver` (not `localhost` in container)
- Check firewall isn't blocking port 1433

### Issue: "Error: Could not connect to server"

**Solution:**
1. Check SQL Server logs: `docker compose logs sqlserver`
2. Verify password is correct in compose.yaml
3. Ensure SA_PASSWORD is at least 8 characters and contains uppercase + digit + special char

### Issue: Database migrations fail

**Solution:**
1. Check API Gateway logs: `docker compose logs apigateway`
2. Verify connection string in appsettings.json
3. Make sure SQL Server is healthy: `docker compose ps`
4. Check database exists: `docker exec -it sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "SELECT * FROM sys.databases"`

### Issue: Port 1433 already in use

**Solution:**
```bash
# Change port in compose.yaml
ports:
  - "1434:1433"  # Use 1434 instead

# Update connection string
Server=localhost,1434;...
```

### Issue: Container exits immediately

**Solution:**
1. Check logs: `docker compose logs apigateway`
2. Verify all environment variables are set correctly
3. Ensure csproj file hasn't changed
4. Rebuild: `docker compose build --no-cache`

## Performance

### Database Performance

- SQL Server Express has limitations: max 10GB database, max 1 CPU core
- For production, upgrade to SQL Server Standard or Enterprise
- Update compose.yaml: `MSSQL_PID: Standard`

### Memory and CPU

Configure resource limits in compose.yaml:

```yaml
services:
  sqlserver:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          cpus: '1'
          memory: 2G
```

## Development vs Production

### Development (compose.yaml)
- SQL Server: Lightweight Express edition
- API Gateway: Production build
- No volumes for code (immutable containers)
- Automatic migrations on startup

### Production Changes

```yaml
# In compose.yaml or docker-compose.prod.yaml

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      MSSQL_PID: Standard  # Change to licensed edition
    volumes:
      - sqlserver_data_prod:/var/opt/mssql  # Separate volume
    deploy:
      resources:
        limits:
          cpus: '4'
          memory: 8G

  apigateway:
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    deploy:
      replicas: 2  # Multiple instances
      resources:
        limits:
          cpus: '2'
          memory: 2G
```

## Cleanup

```bash
# Stop services
docker compose down

# Remove volumes (delete data)
docker compose down -v

# Remove images
docker rmi apigateway sqlserver-db

# Full cleanup (containers, images, volumes, networks)
docker compose down --rmi all -v
```

## Monitoring

### Check Service Health

```bash
# Docker compose
docker compose ps

# Individual logs
docker compose logs sqlserver
docker compose logs apigateway

# Follow logs in real-time
docker compose logs -f --tail 50
```

### Database Monitoring

```bash
# Check database size
docker exec sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "
EXEC sp_databases
"

# Check table sizes
docker exec sqlserver-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourPassword123!" -Q "
SELECT TOP 10 
    t.NAME AS TableName,
    s.Name AS SchemaName,
    p.rows as RowCounts
FROM 
    sys.tables t
    INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
    INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE 
    t.NAME NOT LIKE 'dt%'
GROUP BY t.NAME, s.Name, p.rows
ORDER BY p.rows DESC
"
```

## Next Steps

1. **Start services**: `docker compose up --build`
2. **Verify running**: `docker compose ps`
3. **Check API**: `curl http://localhost:5230/schools`
4. **View logs**: `docker compose logs -f`
5. **Connect to database**: Use SQL Server Management Studio to `localhost,1433`
6. **Deploy changes**: Modify code → `docker compose up --build` → repeat

## References

- [SQL Server Docker Documentation](https://hub.docker.com/_/microsoft-windows-servercore-powershell?tab=description)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [ASP.NET Core in Docker](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)
- [SQL Server Documentation](https://learn.microsoft.com/en-us/sql/sql-server/)
