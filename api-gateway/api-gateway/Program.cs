using api_gateway.Services;
using api_gateway.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<IGatewayService, GatewayService>();
builder.Services.AddHttpClient<IOAuthService, OAuthService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

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
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
})
.WithName("CallExternalAPI")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
