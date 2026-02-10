# API Gateway - SQL Server Setup Guide

## Database Configuration

### Prerequisites
- SQL Server 2019 or later installed locally or accessible remotely
- SQL Server Management Studio (SSMS) or Azure Data Studio for management

### Connection String Setup

Update the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ApiGatewayDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
}
```

**Connection String Parameters:**
- `Server`: SQL Server instance name or IP address
- `Database`: Database name
- `User Id`: SQL Server login (e.g., 'sa' for local)
- `Password`: SQL Server login password
- `TrustServerCertificate=true`: For local development only

### Using Docker SQL Server

For local development, you can run SQL Server in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Then update `appsettings.json` with:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=ApiGatewayDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
}
```

### Database Migrations

The application automatically runs migrations on startup. To manage migrations manually:

**Add a new migration:**
```bash
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

**Remove the latest migration:**
```bash
dotnet ef migrations remove
```

**Apply migrations to database:**
```bash
dotnet ef database update
```

### Database Schema

The application creates the following tables:

**Users Table**
- Id (Primary Key)
- Username (Unique)
- Email (Unique)
- FullName
- PasswordHash
- AuthProvider (Local, Google, Facebook)
- ExternalId (for OAuth providers)
- IsActive
- CreatedAt
- LastLogin

**Roles Table**
- Id (Primary Key)
- Name (Unique)
- Description

**UserRoles Table** (Junction Table)
- UserId (Foreign Key)
- RoleId (Foreign Key)

### Default Roles

The following roles are seeded by default:
- Admin
- User
- Google
- Facebook

### Running the Application

```bash
dotnet run
```

The application will:
1. Restore NuGet packages
2. Build the project
3. Run database migrations automatically
4. Start the server on https://localhost:5230

## Useful SQL Queries

### View all users:
```sql
SELECT * FROM Users;
```

### View user roles:
```sql
SELECT u.Username, r.Name as RoleName 
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UserId
JOIN Roles r ON ur.RoleId = r.Id;
```

### Add role to user:
```sql
INSERT INTO UserRoles (UserId, RoleId) 
VALUES ((SELECT Id FROM Users WHERE Username = 'admin'), 1);
```

## Troubleshooting

**"Cannot open database 'ApiGatewayDb'"**
- Ensure SQL Server is running
- Check connection string in appsettings.json
- Verify database name matches

**Migration fails**
- Run `dotnet ef database drop --force` to reset (development only)
- Then run `dotnet run` to recreate

**Authentication failures**
- Verify user exists in database
- Check PasswordHash is correctly stored
- Use SQL queries above to inspect data
