using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using api_gateway.Models;

namespace api_gateway.Services;

public interface IOAuthService
{
    Task<ExternalAuthResponse> AuthenticateWithGoogleAsync(string idToken);
    Task<ExternalAuthResponse> AuthenticateWithFacebookAsync(string accessToken);
}

public class OAuthService : IOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<OAuthService> _logger;

    public OAuthService(HttpClient httpClient, IConfiguration configuration, IAuthenticationService authService, ILogger<OAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _authService = authService;
        _logger = logger;
    }

    public async Task<ExternalAuthResponse> AuthenticateWithGoogleAsync(string idToken)
    {
        try
        {
            _logger.LogInformation("Authenticating with Google");

            // Verify token with Google
            var googleUser = await VerifyGoogleTokenAsync(idToken);
            if (googleUser == null)
            {
                _logger.LogWarning("Google token verification failed");
                return new ExternalAuthResponse
                {
                    Success = false,
                    Message = "Invalid Google token"
                };
            }

            // Create or get user
            var user = GetOrCreateUser(googleUser.Email, googleUser.Name, "Google");
            var jwtToken = _authService.GenerateToken(user);

            _logger.LogInformation("Google authentication successful for user: {Email}", googleUser.Email);

            return new ExternalAuthResponse
            {
                Success = true,
                Message = "Google authentication successful",
                JwtToken = jwtToken,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google authentication");
            return new ExternalAuthResponse
            {
                Success = false,
                Message = "An error occurred during Google authentication"
            };
        }
    }

    public async Task<ExternalAuthResponse> AuthenticateWithFacebookAsync(string accessToken)
    {
        try
        {
            _logger.LogInformation("Authenticating with Facebook");

            // Get user info from Facebook
            var facebookUser = await GetFacebookUserInfoAsync(accessToken);
            if (facebookUser == null)
            {
                _logger.LogWarning("Facebook token verification failed");
                return new ExternalAuthResponse
                {
                    Success = false,
                    Message = "Invalid Facebook token"
                };
            }

            // Create or get user
            var user = GetOrCreateUser(facebookUser.Email, facebookUser.Name, "Facebook");
            var jwtToken = _authService.GenerateToken(user);

            _logger.LogInformation("Facebook authentication successful for user: {Email}", facebookUser.Email);

            return new ExternalAuthResponse
            {
                Success = true,
                Message = "Facebook authentication successful",
                JwtToken = jwtToken,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Facebook authentication");
            return new ExternalAuthResponse
            {
                Success = false,
                Message = "An error occurred during Facebook authentication"
            };
        }
    }

    private async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            // In production, verify with Google's tokeninfo endpoint
            // For now, we'll do a basic JWT decode and validation
            var handler = new JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(idToken))
            {
                return null;
            }

            var token = handler.ReadJwtToken(idToken);
            
            // Extract claims from Google token
            var googleUser = new GoogleUserInfo
            {
                Id = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                Name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty,
                Picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
            };

            if (string.IsNullOrEmpty(googleUser.Email))
            {
                return null;
            }

            return googleUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google token");
            return null;
        }
    }

    private async Task<FacebookUserInfo?> GetFacebookUserInfoAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, 
                "https://graph.facebook.com/me?fields=id,email,name,picture&access_token=" + accessToken);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook API returned status {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var facebookUser = JsonSerializer.Deserialize<FacebookUserInfo>(content, options);

            return facebookUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Facebook user info");
            return null;
        }
    }

    private UserInfo GetOrCreateUser(string email, string fullName, string provider)
    {
        // In production, this would check/create user in database
        // For now, return a mock user
        var userId = Math.Abs(email.GetHashCode()) % 100000;
        
        return new UserInfo
        {
            Id = userId,
            Username = email.Split('@')[0],
            Email = email,
            FullName = fullName,
            Roles = new List<string> { "User", provider }
        };
    }
}
