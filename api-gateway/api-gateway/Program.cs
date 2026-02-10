using api_gateway.Services;
using api_gateway.Models;
using api_gateway.Data;
using api_gateway.Data.Models;
using api_gateway.Middleware;
using Microsoft.EntityFrameworkCore;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<IGatewayService, GatewayService>();
builder.Services.AddHttpClient<IOAuthService, OAuthService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();

// Add Ocelot to dependency injection
builder.Services.AddOcelot(builder.Configuration);

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiGatewayDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register repository
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Initialize database (run migrations)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiGatewayDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

// Enable subdomain-based school routing (must be before Ocelot)
app.UseSubdomainRouting();

// Configure Ocelot middleware for gateway routes
await app.UseOcelot();

// Login endpoint
app.MapPost("/login", async (LoginRequest request, IAuthenticationService authService) =>
{
    var response = await authService.LoginAsync(request);
    return response.Success ? Results.Ok(response) : Results.Unauthorized();
})
.WithName("Login")
.WithOpenApi();

// Validate token endpoint
app.MapPost("/validate-token", async (string token, IAuthenticationService authService) =>
{
    var isValid = await authService.ValidateTokenAsync(token);
    return isValid ? Results.Ok(new { valid = true }) : Results.Unauthorized();
})
.WithName("ValidateToken")
.WithOpenApi();

// Check user role endpoint
app.MapPost("/check-role", async (string token, string role, IAuthenticationService authService) =>
{
    var hasRole = await authService.HasRoleAsync(token, role);
    return hasRole ? Results.Ok(new { hasRole = true, role = role }) : Results.Ok(new { hasRole = false, role = role });
})
.WithName("CheckRole")
.WithOpenApi();

// Get user roles endpoint
app.MapGet("/user-roles/{username}", async (string username, IAuthenticationService authService) =>
{
    var roles = await authService.GetUserRolesAsync(username);
    return roles.Any() ? Results.Ok(new { username = username, roles = roles }) : Results.NotFound();
})
.WithName("GetUserRoles")
.WithOpenApi();

// Google OAuth endpoint
app.MapPost("/auth/google", async (ExternalAuthRequest request, IOAuthService oauthService) =>
{
    if (string.IsNullOrEmpty(request.Token))
        return Results.BadRequest("Token is required");

    var response = await oauthService.AuthenticateWithGoogleAsync(request.Token);
    return response.Success ? Results.Ok(response) : Results.Unauthorized();
})
.WithName("AuthenticateWithGoogle")
.WithOpenApi();

// Facebook OAuth endpoint
app.MapPost("/auth/facebook", async (ExternalAuthRequest request, IOAuthService oauthService) =>
{
    if (string.IsNullOrEmpty(request.Token))
        return Results.BadRequest("Token is required");

    var response = await oauthService.AuthenticateWithFacebookAsync(request.Token);
    return response.Success ? Results.Ok(response) : Results.Unauthorized();
})
.WithName("AuthenticateWithFacebook")
.WithOpenApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};


// Gateway endpoint example
app.MapGet("/gateway/call", async (IGatewayService gateway, string? url) =>
{
    if (string.IsNullOrEmpty(url))
        return Results.BadRequest("URL parameter is required");

    try
    {
        var result = await gateway.GetAsync<object>(url);
        return Results.Ok(result);
    }
    catch (Exception)
    {
        return Results.StatusCode(500);
    }
})
.WithName("CallExternalAPI")
.WithOpenApi();

// Gateway backends status
app.MapGet("/gateway/backends", (IGatewayService gateway) =>
{
    var backendInfo = new
    {
        backends = new[]
        {
            new { url = "https://jsonplaceholder.typicode.com", status = "healthy" },
            new { url = "https://jsonplaceholder.typicode.com", status = "healthy" }
        },
        totalBackends = 2,
        timestamp = DateTime.UtcNow
    };
    return Results.Ok(backendInfo);
})
.WithName("GetBackendStatus")
.WithOpenApi();

// License Service Endpoints

// Create License
app.MapPost("/license/create", async (int userId, string licenseType, int expiryDays, int maxDevices, ILicenseService licenseService) =>
{
    try
    {
        var license = await licenseService.CreateLicenseAsync(userId, licenseType, expiryDays, maxDevices);
        return Results.Ok(new { success = true, license });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
})
.WithName("CreateLicense")
.WithOpenApi();

// Get License by User ID
app.MapGet("/license/user/{userId}", async (int userId, ILicenseService licenseService) =>
{
    var license = await licenseService.GetLicenseByUserIdAsync(userId);
    return license != null ? Results.Ok(license) : Results.NotFound();
})
.WithName("GetLicenseByUserId")
.WithOpenApi();

// Get License by Key
app.MapGet("/license/key/{licenseKey}", async (string licenseKey, ILicenseService licenseService) =>
{
    var license = await licenseService.GetLicenseByKeyAsync(licenseKey);
    return license != null ? Results.Ok(license) : Results.NotFound();
})
.WithName("GetLicenseByKey")
.WithOpenApi();

// Validate License
app.MapGet("/license/validate/{licenseKey}", async (string licenseKey, ILicenseService licenseService) =>
{
    var isValid = await licenseService.IsLicenseValidAsync(licenseKey);
    return Results.Ok(new { licenseKey, isValid });
})
.WithName("ValidateLicense")
.WithOpenApi();

// Renew License
app.MapPost("/license/renew/{licenseId}", async (int licenseId, int expiryDays, ILicenseService licenseService) =>
{
    var result = await licenseService.RenewLicenseAsync(licenseId, expiryDays);
    return result ? Results.Ok(new { success = true, message = "License renewed successfully" }) 
        : Results.NotFound(new { success = false, message = "License not found" });
})
.WithName("RenewLicense")
.WithOpenApi();

// Suspend License
app.MapPost("/license/suspend/{licenseId}", async (int licenseId, ILicenseService licenseService) =>
{
    var result = await licenseService.SuspendLicenseAsync(licenseId);
    return result ? Results.Ok(new { success = true, message = "License suspended successfully" }) 
        : Results.NotFound(new { success = false, message = "License not found" });
})
.WithName("SuspendLicense")
.WithOpenApi();

// Revoke License
app.MapPost("/license/revoke/{licenseId}", async (int licenseId, ILicenseService licenseService) =>
{
    var result = await licenseService.RevokeLicenseAsync(licenseId);
    return result ? Results.Ok(new { success = true, message = "License revoked successfully" }) 
        : Results.NotFound(new { success = false, message = "License not found" });
})
.WithName("RevokeLicense")
.WithOpenApi();

// Get Licenses by Status
app.MapGet("/license/status/{status}", async (string status, ILicenseService licenseService) =>
{
    var licenses = await licenseService.GetLicensesByStatusAsync(status);
    return Results.Ok(new { status, count = licenses.Count, licenses });
})
.WithName("GetLicensesByStatus")
.WithOpenApi();

// Get Expired Licenses
app.MapGet("/license/expired", async (ILicenseService licenseService) =>
{
    var licenses = await licenseService.GetExpiredLicensesAsync();
    return Results.Ok(new { count = licenses.Count, licenses });
})
.WithName("GetExpiredLicenses")
.WithOpenApi();

// Add Device to License
// Add Assigned User to License
app.MapPost("/license/{licenseId}/user/add", async (int licenseId, ILicenseService licenseService) =>
{
    var result = await licenseService.AddUserAsync(licenseId);
    return result ? Results.Ok(new { success = true, message = "User assigned successfully" }) 
        : Results.BadRequest(new { success = false, message = "Failed to assign user (license not found or max assignments reached)" });
})
.WithName("AddUserToLicense")
.WithOpenApi();

// Remove Assigned User from License
app.MapPost("/license/{licenseId}/user/remove", async (int licenseId, ILicenseService licenseService) =>
{
    var result = await licenseService.RemoveUserAsync(licenseId);
    return result ? Results.Ok(new { success = true, message = "User removed successfully" }) 
        : Results.BadRequest(new { success = false, message = "Failed to remove user" });
})
.WithName("RemoveUserFromLicense")
.WithOpenApi();

// Get License Audit Logs
app.MapGet("/license/{licenseId}/audit", async (int licenseId, ILicenseService licenseService) =>
{
    var logs = await licenseService.GetLicenseAuditLogsAsync(licenseId);
    return Results.Ok(new { licenseId, count = logs.Count, logs });
})
.WithName("GetLicenseAuditLogs")
.WithOpenApi();

// School endpoints
app.MapPost("/schools", async (School school, ISchoolService schoolService) =>
{
    try
    {
        var created = await schoolService.CreateSchoolAsync(school);
        return Results.Created($"/schools/{created.Id}", created);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
})
.WithName("CreateSchool")
.WithOpenApi();

app.MapGet("/schools", async (ISchoolService schoolService) =>
{
    var list = await schoolService.GetAllSchoolsAsync();
    return Results.Ok(list);
})
.WithName("GetAllSchools")
.WithOpenApi();

app.MapGet("/schools/{id}", async (int id, ISchoolService schoolService) =>
{
    var school = await schoolService.GetSchoolByIdAsync(id);
    return school != null ? Results.Ok(school) : Results.NotFound();
})
.WithName("GetSchoolById")
.WithOpenApi();

app.MapPut("/schools/{id}", async (int id, School school, ISchoolService schoolService) =>
{
    if (id != school.Id) return Results.BadRequest("ID mismatch");
    try
    {
        var updated = await schoolService.UpdateSchoolAsync(school);
        return Results.Ok(updated);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
})
.WithName("UpdateSchool")
.WithOpenApi();

app.MapDelete("/schools/{id}", async (int id, ISchoolService schoolService) =>
{
    var removed = await schoolService.DeleteSchoolAsync(id);
    return removed ? Results.Ok(new { success = true }) : Results.NotFound();
})
.WithName("DeleteSchool")
.WithOpenApi();

app.MapGet("/schools/city/{city}", async (string city, ISchoolService schoolService) =>
{
    var list = await schoolService.GetSchoolsByCityAsync(city);
    return Results.Ok(list);
})
.WithName("GetSchoolsByCity")
.WithOpenApi();


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

