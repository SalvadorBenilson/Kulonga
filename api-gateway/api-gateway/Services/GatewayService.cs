using System.Net.Http.Headers;

namespace api_gateway.Services;

public interface IGatewayService
{
    Task<T?> GetAsync<T>(string url);
    Task<T?> PostAsync<T>(string url, object? data = null);
    Task<T?> PutAsync<T>(string url, object? data = null);
    Task<bool> DeleteAsync(string url);
}

public class GatewayService : IGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GatewayService> _logger;

    public GatewayService(HttpClient httpClient, ILogger<GatewayService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            _logger.LogInformation("GET request to: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Url}", url);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object? data = null)
    {
        try
        {
            _logger.LogInformation("POST request to: {Url}", url);
            
            HttpContent? content = null;
            if (data != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<T>(responseContent, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST {Url}", url);
            throw;
        }
    }

    public async Task<T?> PutAsync<T>(string url, object? data = null)
    {
        try
        {
            _logger.LogInformation("PUT request to: {Url}", url);
            
            HttpContent? content = null;
            if (data != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<T>(responseContent, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PUT {Url}", url);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string url)
    {
        try
        {
            _logger.LogInformation("DELETE request to: {Url}", url);
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DELETE {Url}", url);
            throw;
        }
    }
}
