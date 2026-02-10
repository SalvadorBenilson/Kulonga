using api_gateway.Data;
using api_gateway.Services;
using Microsoft.EntityFrameworkCore;

namespace api_gateway.Middleware;

/// <summary>
/// Middleware to extract subdomain from the request host and resolve it to a specific school.
/// Enables routing like: school-name.domain.com -> School with subdomain "school-name"
/// </summary>
public class SubdomainRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubdomainRoutingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SubdomainRoutingMiddleware(RequestDelegate next, ILogger<SubdomainRoutingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        
        // Extract subdomain from host
        var subdomain = ExtractSubdomain(host);

        if (!string.IsNullOrEmpty(subdomain) && subdomain != "www")
        {
            try
            {
                // Resolve the school from database
                using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApiGatewayDbContext>();
                
                var school = await dbContext.Schools
                    .FirstOrDefaultAsync(s => s.Subdomain == subdomain);

                if (school != null)
                {
                    // Add school context to HttpContext.Items for access in endpoints
                    context.Items["School"] = school;
                    context.Items["SchoolId"] = school.Id;
                    context.Items["SchoolName"] = school.Name;
                    context.Items["SchoolSubdomain"] = subdomain;

                    _logger.LogInformation("Subdomain routing: {Subdomain} resolved to school {SchoolName} (ID: {SchoolId})",
                        subdomain, school.Name, school.Id);
                }
                else
                {
                    _logger.LogWarning("Subdomain {Subdomain} not found in database", subdomain);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving subdomain {Subdomain}", subdomain);
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Extracts the subdomain from a hostname.
    /// Example: "school1.domain.com" -> "school1"
    ///          "domain.com" -> empty
    ///          "localhost:5000" -> empty
    /// </summary>
    private static string ExtractSubdomain(string host)
    {
        if (string.IsNullOrEmpty(host))
            return string.Empty;

        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];

        // Split by dots
        var parts = hostWithoutPort.Split('.', StringSplitOptions.RemoveEmptyEntries);

        // If less than 2 parts, it's likely local (localhost) or invalid
        if (parts.Length < 2)
            return string.Empty;

        // If 2+ parts, the first part is the subdomain (unless it's "www")
        var subdomain = parts[0];

        // Don't treat "www" as a subdomain
        if (subdomain.Equals("www", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return subdomain.ToLowerInvariant();
    }
}

/// <summary>
/// Extension method to register SubdomainRoutingMiddleware
/// </summary>
public static class SubdomainRoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseSubdomainRouting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubdomainRoutingMiddleware>();
    }
}
