# Ocelot Subdomain Configuration - Summary of Changes

## Overview

Your API Gateway has been configured to support **school-specific subdomains** using Ocelot. This enables routing requests to specific schools based on subdomain (e.g., `schoolname.yourdomain.com`).

## Changes Made

### 1. **Enhanced School Model** 
   **File**: [api-gateway/Data/Models/SchoolModels.cs](api-gateway/Data/Models/SchoolModels.cs)
   - Added `Subdomain` nullable string property for school identification
   - Allows each school to have a unique subdomain identifier

### 2. **Ocelot Gateway Configuration**
   **File**: [api-gateway/ocelot.json](api-gateway/ocelot.json)
   - Created complete Ocelot routing configuration
   - **SubdomainRoute**: Routes wildcard subdomains (`*.domain.com`) with priority 1
   - **DefaultRoute**: Routes main domain requests with priority 2
   - Includes global Ocelot settings (URL, headers, rate limiting, etc.)

### 3. **Subdomain Routing Middleware**
   **File**: [api-gateway/Middleware/SubdomainRoutingMiddleware.cs](api-gateway/Middleware/SubdomainRoutingMiddleware.cs)
   - Custom middleware to extract subdomains from request hosts
   - Resolves subdomains to schools in the database
   - Adds school context to `HttpContext.Items` for endpoint access
   - Handles edge cases (www, localhost, ports, etc.)

### 4. **Program.cs Updates**
   **File**: [api-gateway/Program.cs](api-gateway/Program.cs)
   - Added Ocelot configuration loading from `ocelot.json`
   - Registered new `SubdomainRoutingMiddleware`
   - Added `app.UseSubdomainRouting()` before Ocelot middleware
   - Fixed code organization and removed duplicate lines

### 5. **Database Migration**
   **File**: `api-gateway/Data/Migrations/[timestamp]_AddSubdomainToSchool.cs`
   - Created Entity Framework migration for the Subdomain column
   - Run `dotnet ef database update` to apply changes

### 6. **Comprehensive Documentation**
   **File**: [SUBDOMAIN_CONFIGURATION.md](SUBDOMAIN_CONFIGURATION.md)
   - Detailed setup and configuration guide
   - Architecture overview
   - DNS configuration examples
   - Local development instructions
   - Troubleshooting guide
   - Best practices and security notes

### 7. **Code Examples**
   **File**: [api-gateway/Examples/SubdomainEndpointExamples.cs](api-gateway/Examples/SubdomainEndpointExamples.cs)
   - 6 practical examples of using subdomain context in endpoints
   - Helper extension methods (`GetSchoolId()`, `GetSchool()`, etc.)
   - Pattern for school-specific validation
   - Logging integration示例

## How It Works

### Request Flow

```
Client Request (https://lincoln.domain.com/api/users)
         ↓
DNS Resolution
         ↓
API Gateway (HTTPS)
         ↓
SubdomainRoutingMiddleware
  - Extracts subdomain: "lincoln"
  - Queries database for matching School
  - Populates HttpContext.Items with School data
         ↓
Ocelot Gateway Routes Request
  - Matches wildcard route: *.domain.com
  - Routes to downstream service
         ↓
Endpoint/Controller Access
  - Retrieves school context from HttpContext.Items
  - Processes request with school-specific data
         ↓
Response Returned
```

### Accessing School Context in Endpoints

```csharp
app.MapGet("/api/data", (HttpContext context) =>
{
    // Using direct access
    var schoolId = (int)context.Items["SchoolId"];
    var school = context.Items["School"] as School;

    // Or using extension methods (safer)
    var schoolId = context.GetSchoolId();
    var school = context.GetSchool();

    return Results.Ok(new { schoolId, data = "..." });
});
```

## Next Steps

### 1. **Configure Your Domain**
Update `ocelot.json` with your actual domain:
```json
"Host": "*.yourdomain.com"
```

### 2. **Set Up DNS Records**
```
*.yourdomain.com    A    your.api.gateway.ip
yourdomain.com      A    your.api.gateway.ip
```

### 3. **Add School Subdomains**
```csharp
var school = new School
{
    Name = "Lincoln High School",
    Code = "LHS",
    Subdomain = "lincoln",  // ← Add this for subdomain routing
    Email = "admin@lincolnhigh.edu"
};
await schoolService.CreateSchoolAsync(school);
```

### 4. **Test Locally** (Optional)
Update your `/etc/hosts` file:
```
127.0.0.1 school1.local.com
127.0.0.1 school2.local.com
```

Then access:
```
https://school1.local.com:5001/api/users
```

### 5. **Run Database Migration**
```bash
dotnet ef database update
```

### 6. **Deploy and Test**
```bash
dotnet build
dotnet publish
```

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Requests                           │
│              (*.domain.com | domain.com)                     │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │   HTTPS Load Balancer │
         └────────┬──────────────┘
                  │
                  ▼
      ┌──────────────────────────┐
      │  API Gateway (Ocelot)    │
      └────────┬─────────────────┘
               │
               ▼
    ┌────────────────────────┐
    │ SubdomainRouting       │
    │ Middleware             │
    │ - Extract subdomain    │
    │ - Resolve to School    │
    │ - Add to HttpContext   │
    └────────┬───────────────┘
             │
             ▼
    ┌────────────────────────┐
    │ Ocelot Routes          │
    │ - Subdomain (*)        │
    │ - Default              │
    └────────┬───────────────┘
             │
             ▼
    ┌────────────────────────┐
    │ Downstream Services    │
    │ - School API           │
    │ - Auth Service         │
    │ - License Service      │
    │ - etc.                 │
    └────────────────────────┘
```

## Key Features

✅ **Wildcard Subdomain Routing** - Route any subdomain to school-specific backends  
✅ **Database-Driven** - School subdomains stored and queried from database  
✅ **Context Isolation** - Each request includes school context automatically  
✅ **Flexible Downstream Routing** - Route to different services per school  
✅ **Performance Optimized** - Minimal overhead with extensible caching  
✅ **Production Ready** - Includes error handling, logging, and security best practices  
✅ **Well Documented** - Comprehensive guides and code examples  

## File Structure

```
api-gateway/
├── ocelot.json                                 (Gateway config)
├── Program.cs                                  (Updated with middleware)
├── Data/
│   ├── Models/SchoolModels.cs                 (Added Subdomain)
│   └── Migrations/
│       └── AddSubdomainToSchool.cs            (New migration)
├── Middleware/
│   └── SubdomainRoutingMiddleware.cs          (New middleware)
├── Examples/
│   └── SubdomainEndpointExamples.cs           (Usage examples)
├── SUBDOMAIN_CONFIGURATION.md                 (Detailed guide)
└── [...other files...]
```

## Testing

### Quick Test After Setup

```bash
# 1. Start the API Gateway
dotnet run

# 2. In another terminal, test with a subdomain
curl -H "Host: schoolname.domain.com" https://localhost:5001/api/test

# 3. Or using an actual domain if DNS is configured
curl https://schoolname.yourdomain.com/api/test
```

### Verify School Context

```csharp
app.MapGet("/debug/context", (HttpContext context) =>
{
    return Results.Ok(new
    {
        hasSchoolContext = context.Items.ContainsKey("SchoolId"),
        schoolId = context.Items["SchoolId"],
        schoolName = context.Items["SchoolName"],
        subdomain = context.Items["SchoolSubdomain"]
    });
});

// Access: https://schoolname.domain.com/debug/context
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Subdomain not resolving | Check DNS records, verify school exists in DB with matching subdomain |
| 404 errors on subdomain requests | Ensure ocelot.json Host matches your domain |
| School context is null | Verify subdomain is set on School record and DNS is configured |
| Development testing fails | Update /etc/hosts with test domains |

See [SUBDOMAIN_CONFIGURATION.md](SUBDOMAIN_CONFIGURATION.md) for detailed troubleshooting.

## Build Status

✅ **Solution builds successfully** with 2 minor async warnings (non-blocking)

```
Build succeeded with 2 warning(s) in 2.5s
```

## Important Notes

- The `Subdomain` property on the School model is optional (nullable)
- Existing schools won't have subdomains assigned until you explicitly set them
- The middleware gracefully handles requests without valid subdomains
- Ocelot continues to route default domain requests as before
- All existing endpoints remain functional

## Support Files

- **Main Configuration**: [ocelot.json](api-gateway/ocelot.json)
- **Middleware**: [SubdomainRoutingMiddleware.cs](api-gateway/Middleware/SubdomainRoutingMiddleware.cs)
- **Examples**: [SubdomainEndpointExamples.cs](api-gateway/Examples/SubdomainEndpointExamples.cs)
- **Full Guide**: [SUBDOMAIN_CONFIGURATION.md](SUBDOMAIN_CONFIGURATION.md)

---

**Configuration is complete!** Your API Gateway is now ready for school-specific subdomain routing. Follow the setup instructions in the documentation to get this live.
