using System.Net.Http.Headers;
using System.Threading;
using System.Collections.Generic;
using api_gateway.Data.Models;

namespace api_gateway.Services;

public interface IGatewayService
{
    Task<T?> GetAsync<T>(string url);
    Task<T?> PostAsync<T>(string url, object? data = null);
    Task<T?> PutAsync<T>(string url, object? data = null);
    Task<bool> DeleteAsync(string url);
}

internal class BackendEntry
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsHealthy { get; set; } = true;
    public DateTime UnhealthyUntil { get; set; } = DateTime.MinValue;
}

public class GatewayService : IGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GatewayService> _logger;
    private readonly BackendEntry[] _backends;
    private readonly int _timeoutSeconds;
    private int _counter;

    public GatewayService(HttpClient httpClient, ILogger<GatewayService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        var urls = config.GetSection("Gateway:Backends").Get<string[]>() ?? Array.Empty<string>();
        _backends = urls.Select(u => new BackendEntry { BaseUrl = u.TrimEnd('/') }).ToArray();

        _timeoutSeconds = config.GetValue<int?>("Gateway:TimeoutSeconds") ?? 10;
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    private BackendEntry PickBackend(string originalUrl)
    {
        if (_backends == null || _backends.Length == 0)
            throw new InvalidOperationException("No backends configured for gateway");

        var len = _backends.Length;
        for (int i = 0; i < len; i++)
        {
            var idx = Math.Abs(Interlocked.Increment(ref _counter)) % len;
            var candidate = _backends[idx];
            if (candidate.IsHealthy || candidate.UnhealthyUntil <= DateTime.UtcNow)
            {
                // treat as healthy (if past unhealthy window)
                return candidate;
            }
        }

        // fallback to deterministic choice
        var fallback = _backends[Math.Abs(Interlocked.Increment(ref _counter)) % len];
        return fallback;
    }

    private string BuildTargetUrl(BackendEntry backend, string originalUrl)
    {
        if (Uri.TryCreate(originalUrl, UriKind.Absolute, out var incoming))
        {
            // use path+query from incoming and apply to backend base
            return backend.BaseUrl + incoming.PathAndQuery;
        }

        // originalUrl is a relative path
        return backend.BaseUrl + (originalUrl.StartsWith("/") ? originalUrl : "/" + originalUrl);
    }

    private void MarkBackendUnhealthy(BackendEntry backend)
    {
        backend.IsHealthy = false;
        backend.UnhealthyUntil = DateTime.UtcNow.AddSeconds(30); // simple cooldown
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        Exception? lastEx = null;
        for (int attempt = 0; attempt < _backends.Length; attempt++)
        {
            var backend = PickBackend(url);
            var target = BuildTargetUrl(backend, url);
            try
            {
                _logger.LogInformation("Gateway GET -> {Target}", target);
                var response = await _httpClient.GetAsync(target);
                if (!response.IsSuccessStatusCode)
                {
                    MarkBackendUnhealthy(backend);
                    lastEx = new HttpRequestException($"Status: {response.StatusCode}");
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend {BaseUrl} failed for GET", backend.BaseUrl);
                MarkBackendUnhealthy(backend);
                lastEx = ex;
                continue;
            }
        }

        _logger.LogError(lastEx, "All backends failed for GET {Url}", url);
        throw lastEx ?? new InvalidOperationException("All backends failed");
    }

    public async Task<T?> PostAsync<T>(string url, object? data = null)
    {
        Exception? lastEx = null;
        for (int attempt = 0; attempt < _backends.Length; attempt++)
        {
            var backend = PickBackend(url);
            var target = BuildTargetUrl(backend, url);
            try
            {
                _logger.LogInformation("Gateway POST -> {Target}", target);
                HttpContent? content = null;
                if (data != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(data);
                    content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.PostAsync(target, content);
                if (!response.IsSuccessStatusCode)
                {
                    MarkBackendUnhealthy(backend);
                    lastEx = new HttpRequestException($"Status: {response.StatusCode}");
                    continue;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(responseContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend {BaseUrl} failed for POST", backend.BaseUrl);
                MarkBackendUnhealthy(backend);
                lastEx = ex;
                continue;
            }
        }

        _logger.LogError(lastEx, "All backends failed for POST {Url}", url);
        throw lastEx ?? new InvalidOperationException("All backends failed");
    }

    public async Task<T?> PutAsync<T>(string url, object? data = null)
    {
        Exception? lastEx = null;
        for (int attempt = 0; attempt < _backends.Length; attempt++)
        {
            var backend = PickBackend(url);
            var target = BuildTargetUrl(backend, url);
            try
            {
                _logger.LogInformation("Gateway PUT -> {Target}", target);
                HttpContent? content = null;
                if (data != null)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(data);
                    content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.PutAsync(target, content);
                if (!response.IsSuccessStatusCode)
                {
                    MarkBackendUnhealthy(backend);
                    lastEx = new HttpRequestException($"Status: {response.StatusCode}");
                    continue;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(responseContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend {BaseUrl} failed for PUT", backend.BaseUrl);
                MarkBackendUnhealthy(backend);
                lastEx = ex;
                continue;
            }
        }

        _logger.LogError(lastEx, "All backends failed for PUT {Url}", url);
        throw lastEx ?? new InvalidOperationException("All backends failed");
    }

    public async Task<bool> DeleteAsync(string url)
    {
        Exception? lastEx = null;
        for (int attempt = 0; attempt < _backends.Length; attempt++)
        {
            var backend = PickBackend(url);
            var target = BuildTargetUrl(backend, url);
            try
            {
                _logger.LogInformation("Gateway DELETE -> {Target}", target);
                var response = await _httpClient.DeleteAsync(target);
                if (!response.IsSuccessStatusCode)
                {
                    MarkBackendUnhealthy(backend);
                    lastEx = new HttpRequestException($"Status: {response.StatusCode}");
                    continue;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend {BaseUrl} failed for DELETE", backend.BaseUrl);
                MarkBackendUnhealthy(backend);
                lastEx = ex;
                continue;
            }
        }

        _logger.LogError(lastEx, "All backends failed for DELETE {Url}", url);
        throw lastEx ?? new InvalidOperationException("All backends failed");
    }
}
