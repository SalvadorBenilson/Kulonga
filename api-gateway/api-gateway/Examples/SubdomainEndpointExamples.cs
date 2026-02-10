using api_gateway.Data.Models;
using api_gateway.Services;

namespace api_gateway.Examples;

/// <summary>
/// Example: Using Subdomain Context in Endpoints
/// This file demonstrates how to access school context from subdomains in your endpoints.
/// </summary>
public static class SubdomainEndpointExamples
{
    /// <summary>
    /// Example 1: Get students for the current school subdomain
    /// Usage: GET https://lincoln.domain.com/api/school-students
    /// </summary>
    public static void MapSchoolSpecificStudents(this WebApplication app)
    {
        app.MapGet("/api/school-students", (HttpContext context) =>
        {
            // Get school from context populated by SubdomainRoutingMiddleware
            if (!context.Items.TryGetValue("SchoolId", out var schoolIdObj) || schoolIdObj is not int schoolId)
            {
                return Results.BadRequest(new { error = "School context not found. Request must come from a school subdomain." });
            }

            var schoolName = context.Items["SchoolName"]?.ToString() ?? "Unknown";
            var subdomain = context.Items["SchoolSubdomain"]?.ToString() ?? "unknown";

            // TODO: Fetch students for this specific school
            // var students = await studentService.GetStudentsBySchoolAsync(schoolId);

            return Results.Ok(new
            {
                message = $"Students for {schoolName}",
                schoolId = schoolId,
                schoolName = schoolName,
                subdomain = subdomain,
                // students = students
            });
        })
        .WithName("GetSchoolStudents")
        .WithOpenApi()
        .Produces<object>(200)
        .Produces<object>(400);
    }

    /// <summary>
    /// Example 2: Create a user in the context of a school subdomain
    /// Usage: POST https://jefferson.domain.com/api/school-user
    /// </summary>
    public static void MapCreateSchoolUser(this WebApplication app)
    {
        app.MapPost("/api/school-user", async (HttpContext context, CreateUserRequest request) =>
        {
            // Validate school context
            var school = context.Items["School"] as School;
            if (school == null)
            {
                return Results.BadRequest(new { error = "Invalid school context" });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest(new { error = "Username is required" });
            }

            // TODO: Create user associated with this school
            // var newUser = new User
            // {
            //     Username = request.Username,
            //     SchoolId = school.Id,
            //     Email = request.Email
            // };
            // await userService.CreateUserAsync(newUser);

            return Results.Created($"/api/school-user/{request.Username}", new
            {
                message = "User created successfully",
                username = request.Username,
                schoolName = school.Name,
                schoolId = school.Id
            });
        })
        .WithName("CreateSchoolUser")
        .WithOpenApi()
        .Produces<object>(201)
        .Produces<object>(400)
        .Produces<object>(401);
    }

    /// <summary>
    /// Example 3: Get dashboard data filtered by school
    /// Usage: GET https://washington.domain.com/api/school-dashboard
    /// </summary>
    public static void MapSchoolDashboard(this WebApplication app)
    {
        app.MapGet("/api/school-dashboard", (HttpContext context) =>
        {
            var schoolId = context.Items["SchoolId"] as int?;
            var schoolName = context.Items["SchoolName"]?.ToString();

            if (schoolId == null)
            {
                return Results.BadRequest(new { error = "School subdomain required" });
            }

            // TODO: Get dashboard metrics for this school
            // var dashboardData = await dashboardService.GetDashboardDataAsync(schoolId.Value);

            return Results.Ok(new
            {
                schoolId = schoolId,
                schoolName = schoolName,
                dashboardData = new
                {
                    totalStudents = 450,
                    totalTeachers = 35,
                    totalClasses = 18,
                    averageAttendance = 94.5
                }
            });
        })
        .WithName("GetSchoolDashboard")
        .WithOpenApi()
        .Produces<object>(200)
        .Produces<object>(400);
    }

    /// <summary>
    /// Example 4: Admin endpoint that requires both subdomain and admin role
    /// Usage: DELETE https://lincoln.domain.com/api/school-admin/cleanup
    /// </summary>
    public static void MapSchoolAdminOperation(this WebApplication app)
    {
        app.MapDelete("/api/school-admin/cleanup", async (HttpContext context, IAuthenticationService authService) =>
        {
            // Verify school context
            var schoolId = context.Items["SchoolId"] as int?;
            if (schoolId == null)
            {
                return Results.BadRequest(new { error = "School context required" });
            }

            // Verify authorization header
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Results.BadRequest(new { error = "Authorization token required" });
            }

            // Extract token and verify admin role
            var token = authHeader.Replace("Bearer ", "");
            var isAdmin = await authService.HasRoleAsync(token, "admin");

            if (!isAdmin)
            {
                return Results.Forbid();
            }

            // TODO: Perform admin operation for this school
            // await adminService.CleanupSchoolDataAsync(schoolId.Value);

            return Results.Ok(new
            {
                message = "School cleanup completed successfully",
                schoolId = schoolId
            });
        })
        .WithName("SchoolAdminCleanup")
        .WithOpenApi()
        .Produces<object>(200)
        .Produces<object>(401)
        .Produces<object>(403);
    }

    /// <summary>
    /// Example 5: Middleware-like pattern for school-specific validation
    /// Shows how to create a reusable pattern for protecting school endpoints
    /// </summary>
    public static IResult RequireSchoolContext(HttpContext context)
    {
        var school = context.Items["School"] as School;
        if (school == null)
        {
            return Results.BadRequest(new
            {
                error = "Invalid school context",
                hint = "This endpoint requires a school subdomain (e.g., schoolname.domain.com)"
            });
        }

        return Results.Ok();
    }

    /// <summary>
    /// Example 6: Logging with school context
    /// Shows how to enhance logging with school information
    /// </summary>
    public static void MapLoggingExample(this WebApplication app)
    {
        app.MapGet("/api/example-with-logging", (HttpContext context) =>
        {
            var schoolId = context.Items["SchoolId"];
            var schoolName = context.Items["SchoolName"];
            var subdomain = context.Items["SchoolSubdomain"];

            return Results.Ok(new
            {
                message = "Logged request details"
            });
        })
        .WithName("ExampleWithLogging")
        .WithOpenApi();
    }
}

/// <summary>
/// Example request models used in endpoints above
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// Example of a helper extension method for safer school context access
/// </summary>
public static class SchoolContextExtensions
{
    /// <summary>
    /// Safely gets the current school ID from context
    /// </summary>
    public static int? GetSchoolId(this HttpContext context)
    {
        return context.Items["SchoolId"] as int?;
    }

    /// <summary>
    /// Safely gets the current school object from context
    /// </summary>
    public static School? GetSchool(this HttpContext context)
    {
        return context.Items["School"] as School;
    }

    /// <summary>
    /// Safely gets the current school name from context
    /// </summary>
    public static string? GetSchoolName(this HttpContext context)
    {
        return context.Items["SchoolName"]?.ToString();
    }

    /// <summary>
    /// Safely gets the current subdomain from context
    /// </summary>
    public static string? GetSubdomain(this HttpContext context)
    {
        return context.Items["SchoolSubdomain"]?.ToString();
    }

    /// <summary>
    /// Checks if a valid school context exists
    /// </summary>
    public static bool HasSchoolContext(this HttpContext context)
    {
        return context.Items.ContainsKey("SchoolId") && context.Items["SchoolId"] is int;
    }
}

/// <summary>
/// Registration helper - Add these examples to your Program.cs
/// </summary>
public static class SubdomainExamplesRegistration
{
    public static void RegisterSubdomainEndpointExamples(this WebApplication app)
    {
        // Uncomment to register example endpoints:
        // app.MapSchoolSpecificStudents();
        // app.MapCreateSchoolUser();
        // app.MapSchoolDashboard();
        // app.MapSchoolAdminOperation();
        // app.MapLoggingExample(app.Services.GetRequiredService<ILogger<Program>>());
    }
}
