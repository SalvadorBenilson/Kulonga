# Ocelot Subdomain Configuration Guide

## Overview

The API Gateway is now configured to support **school-specific subdomains**. This allows each school to have its own namespace through subdomain routing (e.g., `schoolname.domain.com` routes to a specific school's backend services).

## Architecture

### Components

1. **Ocelot.json** - Gateway routing configuration with subdomain support
2. **SubdomainRoutingMiddleware** - Custom middleware to extract and resolve subdomains to schools
3. **School Model** - Enhanced with a `Subdomain` field for school identification
4. **Database Migration** - `AddSubdomainToSchool` migration to persist subdomain data

## Configuration Details

### 1. Ocelot Routes (ocelot.json)

The `ocelot.json` file contains two main route configurations:

#### Route 1: Subdomain-Based Routing
```json
{
  "DownstreamPathTemplate": "/{everything}",
  "UpstreamPathTemplate": "/{everything}",
  "HostAndPorts": [
    {
      "Host": "*.domain.com",
      "Port": 443
    }
  ],
  "Key": "SubdomainRoute"
}
```

- **Matches**: Requests to `*.domain.com` (any subdomain except `www`)
- **Routes to**: School-specific downstream service
- **Priority**: 1 (Higher priority)

#### Route 2: Default Routing
```json
{
  "Key": "DefaultRoute",
  "Priority": 2 (Lower priority)
}
```

- **Matches**: Main domain requests
- **Routes to**: Primary API service

### 2. Subdomain Extraction Middleware

The `SubdomainRoutingMiddleware` automatically:

1. **Extracts subdomains** from incoming request hosts
2. **Resolves subdomains** to schools in the database
3. **Adds school context** to `HttpContext.Items` for use in endpoints

#### Example Subdomain Extraction:
| Host | Extracted Subdomain |
|------|-------------------|
| `school1.domain.com` | `school1` |
| `elementary.domain.com` | `elementary` |
| `domain.com` | (empty) |
| `www.domain.com` | (empty) |
| `localhost:5000` | (empty) |

#### Available in HttpContext:
```csharp
var school = context.Items["School"] as School;
var schoolId = (int)context.Items["SchoolId"];
var schoolName = context.Items["SchoolName"]?.ToString();
var subdomain = context.Items["SchoolSubdomain"]?.ToString();
```

## Setup Instructions

### 1. Configure Your Domain

Update the `ocelot.json` file with your actual domain:

```json
"HostAndPorts": [
  {
    "Host": "*.yourdomain.com",
    "Port": 443
  }
]
```

### 2. Configure DNS

Add DNS wildcard records for your domain:

```
*.yourdomain.com    A    your.api.gateway.ip
yourdomain.com      A    your.api.gateway.ip
```

Example with Route53 (AWS):
```
Name: *.yourdomain.com
Type: A
Value: your-load-balancer-dns-name
```

### 3. Update School Records

Add subdomain values to your school records in the database:

```csharp
var school = new School
{
    Name = "Lincoln High School",
    Code = "LHS",
    Subdomain = "lincoln",
    Email = "admin@lincolnhigh.edu"
};

await schoolService.CreateSchoolAsync(school);
```

### 4. Access School-Specific Routes

Once configured, schools can access their resources via subdomains:

```
GET https://lincoln.yourdomain.com/api/users
GET https://washington.yourdomain.com/api/students
GET https://jefferson.yourdomain.com/api/grades
```

## Usage Examples

### Creating a School with Subdomain

```csharp
app.MapPost("/schools", async (School school, ISchoolService schoolService) =>
{
    school.Subdomain = school.Name.ToLower().Replace(" ", "");
    var created = await schoolService.CreateSchoolAsync(school);
    return Results.Created($"/schools/{created.Id}", created);
});
```

### Accessing School Context in Endpoints

```csharp
app.MapGet("/api/students", async (HttpContext context, IStudentService studentService) =>
{
    if (context.Items.TryGetValue("SchoolId", out var schoolId))
    {
        var students = await studentService.GetStudentsBySchoolAsync((int)schoolId);
        return Results.Ok(students);
    }

    return Results.BadRequest("School context not found");
});
```

### Using Subdomain in Logging

```csharp
app.MapGet("/user-roles/{username}", async (string username, IAuthenticationService authService, HttpContext context) =>
{
    var subdomain = context.Items["SchoolSubdomain"]?.ToString() ?? "unknown";
    _logger.LogInformation("User roles requested for {Username} at {Subdomain}", username, subdomain);
    
    var roles = await authService.GetUserRolesAsync(username);
    return Results.Ok(roles);
});
```

## Ocelot Configuration Best Practices

### 1. Multiple Downstream Services

If you have separate services per school:

```json
{
  "DownstreamPathTemplate": "/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    {
      "Host": "school-service-{subdomain}",
      "Port": 5000
    }
  ],
  "UpstreamPathTemplate": "/{everything}",
  "HostAndPorts": [
    {
      "Host": "*.domain.com",
      "Port": 443
    }
  ]
}
```

### 2. Load Balancing

```json
"DownstreamHostAndPorts": [
  { "Host": "school-api-1", "Port": 5000 },
  { "Host": "school-api-2", "Port": 5000 },
  { "Host": "school-api-3", "Port": 5000 }
],
"LoadBalancingPolicy": "RoundRobin"
```

### 3. Rate Limiting Per School

```json
"RateLimitOptions": {
  "ClientIdHeader": "SchoolId",
  "QuotaExceededMessage": "School quota exceeded",
  "RateLimitCounterPrefix": "ocelot_school_rate",
  "HttpStatusCodeOnQuotaExceeded": 429
}
```

### 4. CORS Configuration

```json
"EnableRateLimiting": true,
"AuthenticationOptions": {
  "AuthenticationProviderKey": "Bearer",
  "AllowedScopes": []
},
"SecurityOptions": {
  "IPAllowedList": [],
  "IPBlockedList": [],
  "ExcludeClientIPAcl": false
}
```

## Local Development

### Testing with Hosts File

Update your `/etc/hosts` file (Mac/Linux) or `C:\Windows\System32\drivers\etc\hosts` (Windows):

```
127.0.0.1 localhost
127.0.0.1 school1.local.com
127.0.0.1 school2.local.com
```

Then access via:
```
https://school1.local.com:5001/api/users
https://school2.local.com:5001/api/users
```

### Docker Compose Configuration

If using Docker, ensure network connectivity:

```yaml
services:
  api-gateway:
    container_name: api-gateway
    networks:
      - kulonga-network
    environment:
      - ASPNETCORE_URLS=https://+:443

  school-service:
    networks:
      - kulonga-network

networks:
  kulonga-network:
    driver: bridge
```

## Troubleshooting

### 1. Subdomain Not Resolving

**Check:**
- DNS records are configured
- Subdomain value matches exactly (case-insensitive but consistent)
- School exists in database

```csharp
var schools = await context.Schools
    .Where(s => s.Subdomain != null)
    .ToListAsync();
_logger.LogInformation("Available subdomains: {Subdomains}", 
    string.Join(", ", schools.Select(s => s.Subdomain)));
```

### 2. Middleware Not Applied

**Verify in Program.cs:**
```csharp
app.UseSubdomainRouting(); // Must be before app.UseOcelot()
await app.UseOcelot();
```

### 3. School Context Not Found

**Check middleware logs:**
```csharp
_logger.LogWarning("Subdomain {Subdomain} not found in database", subdomain);
```

### 4. HTTPS Certificate Issues

Ensure your SSL certificate covers wildcard subdomains:
```
Subject: *.domain.com
SubjectAltName: *.domain.com, domain.com
```

## Migration Information

Run the migration to add the Subdomain column:

```bash
dotnet ef database update
```

The migration `AddSubdomainToSchool` adds:
- `Subdomain` nullable string column to `Schools` table
- Index on `Subdomain` for fast lookups (optional, can be added manually)

## Performance Considerations

1. **Database Lookups**: Subdomain resolution requires a DB query. Consider caching:

```csharp
services.AddMemoryCache();

// Cache subdomain-to-school mapping
var cachedSchool = memoryCache.GetOrCreate(subdomain, entry =>
{
    entry.SlidingExpiration = TimeSpan.FromMinutes(30);
    return dbContext.Schools.FirstOrDefault(s => s.Subdomain == subdomain);
});
```

2. **Unique Submarine Constraint**: Add a unique index for subdomains:

```sql
CREATE UNIQUE INDEX IX_Schools_Subdomain ON Schools(Subdomain) 
WHERE Subdomain IS NOT NULL;
```

3. **Connection String**: Ensure your downstream services connection strings account for school-specific data isolation.

## Security Notes

- **Validate subdomains** in the middleware before DB lookup
- **Sanitize** subdomain input to prevent SQL injection (the current implementation uses EF Core parameters, which is safe)
- **Rate limit** per subdomain to prevent abuse
- **Log access** attempts with subdomain information for audit trails

## References

- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware)
- [HttpContext.Items](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.items)
