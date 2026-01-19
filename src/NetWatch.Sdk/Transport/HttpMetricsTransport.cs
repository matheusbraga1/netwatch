using Microsoft.Extensions.Logging;
using NetWatch.Sdk.Configuration;
using NetWatch.Sdk.Models;
using System.Net.Http.Json;

namespace NetWatch.Sdk.Transport;

public class HttpMetricsTransport : IMetricsTransport, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NetWatchOptions _options;
    private readonly ILogger<HttpMetricsTransport> _logger;
    private bool _disposed;

    public HttpMetricsTransport(
        HttpClient htppClient,
        NetWatchOptions options,
        ILogger<HttpMetricsTransport> logger)
    {
        _httpClient = htppClient ?? throw new ArgumentNullException(nameof(htppClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (!string.IsNullOrWhiteSpace(_options.CollectorEndpoint))
        {
            _httpClient.BaseAddress = new Uri(_options.CollectorEndpoint);
        }

        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"NetWatch-SDK/1.0.0");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);

        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> SendBatchAsync(IReadOnlyList<RequestMetric> metrics, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HttpMetricsTransport));

        if (metrics == null || metrics.Count == 0)
        {
            _logger.LogWarning("Attempted to send empty batch");
            return false;
        }

        var request = new MetricsBatchRequest
        {
            Metrics = metrics.ToList(),
            SdkVersion = "1.0.0",
            CreatedAt = DateTime.UtcNow,
            Hostname = Environment.MachineName
        };

        var attempt = 0;
        var maxAttempts = 3;
        var delay = TimeSpan.FromSeconds(1);

        while (attempt < maxAttempts)
        {
            attempt++;

            try
            {
                _logger.LogDebug($"Sending batch with {metrics.Count} metrics (attempt {attempt}/{maxAttempts})");

                var response = await _httpClient.PostAsJsonAsync(
                    "/api/metrics/batch",
                    request,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<MetricsBatchResponse>(cancellationToken: cancellationToken);

                    _logger.LogInformation($"Successfully sent batch: {result?.JobId}, Accepted: {result?.Accepted ?? metrics.Count}");

                    return true;
                }

                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    _logger.LogError($"Client error sending batch: {response.StatusCode} - {response.ReasonPhrase}");

                    return false;
                }

                _logger.LogWarning($"Server error sending batch: {response.StatusCode}. Retrying in {delay.TotalSeconds}s...");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, $"Network error sending batch (attempt {attempt}/{maxAttempts}). Retrying in {delay.TotalSeconds}s...");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning($"Timeout sending batch (attempt {attempt}/{maxAttempts}). Retrying in {delay.TotalSeconds}s...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error sending batch (attempt {attempt}/{maxAttempts})");
            }

            if (attempt < maxAttempts )
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds + 2);
            }
        }

        _logger.LogError($"Failed to send batch after {maxAttempts} attempts. Metrics lost: {metrics.Count}");

        return false;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return false;

        try
        {
            var response = await _httpClient.GetAsync(
                "/health",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed");

            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
