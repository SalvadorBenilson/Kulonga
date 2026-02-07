using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using api_gateway.Models;

namespace api_gateway.Services;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    string GenerateToken(UserInfo user);
    Task<bool> HasRoleAsync(string token, string role);
    Task<List<string>> GetUserRolesAsync(string username);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    // Mock user database - replace with real database in production
    private static readonly List<(string Username, string Password, UserInfo User)> MockUsers = new()
    {
        ("admin", "password123", new UserInfo 
        { 
            Id = 1, 
            Username = "admin", 
            Email = "admin@example.com", 
            FullName = "Administrator",
            Roles = new List<string> { "Admin", "User" }
        }),
        ("user1", "user1pass", new UserInfo 
        { 
            Id = 2, 
            Username = "user1", 
            Email = "user1@example.com", 
            FullName = "User One",
            Roles = new List<string> { "User" }
        })
    };

    public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

            // Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                };
            }

            // Simulate database lookup - replace with real database call
            await Task.Delay(100); // Simulate async operation

            var user = MockUsers.FirstOrDefault(u => 
                u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == request.Password);

            if (user == default)
            {
                _logger.LogWarning("Login failed for user: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            var token = GenerateToken(user.User);
            _logger.LogInformation("Login successful for user: {Username}", request.Username);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = user.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new LoginResponse
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var jwtSecret = _configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                _logger.LogWarning("JWT secret not configured");
                return false;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    public string GenerateToken(UserInfo user)
    {
        var jwtSecret = _configuration["Jwt:Secret"];
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "api-gateway";
        var jwtExpireMins = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var mins) ? mins : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret ?? "your-secret-key-change-this-in-production"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimsList = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("FullName", user.FullName)
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claimsList.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            claims: claimsList,
            expires: DateTime.UtcNow.AddMinutes(jwtExpireMins),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> HasRoleAsync(string token, string role)
    {
        try
        {
            _logger.LogInformation("Checking if token has role: {Role}", role);
            var jwtSecret = _configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                return false;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var roles = principal.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var hasRole = roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("Token has role {Role}: {Result}", role, hasRole);

            return await Task.FromResult(hasRole);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking role in token");
            return false;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(string username)
    {
        try
        {
            _logger.LogInformation("Getting roles for user: {Username}", username);
            
            await Task.Delay(50); // Simulate async operation

            var user = MockUsers.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == default)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return new List<string>();
            }

            return await Task.FromResult(user.User.Roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles");
            return new List<string>();
        }
    }
}
