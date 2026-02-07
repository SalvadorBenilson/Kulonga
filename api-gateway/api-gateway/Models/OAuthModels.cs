namespace api_gateway.Models;

public class ExternalAuthRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ExternalAuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? JwtToken { get; set; }
    public UserInfo? User { get; set; }
}

public class GoogleUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
}

public class FacebookUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Picture? Picture { get; set; }
}

public class Picture
{
    public PictureData? Data { get; set; }
}

public class PictureData
{
    public string? Url { get; set; }
}

public class OAuthSettings
{
    public GoogleOAuthSettings Google { get; set; } = new();
    public FacebookOAuthSettings Facebook { get; set; } = new();
}

public class GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class FacebookOAuthSettings
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}
