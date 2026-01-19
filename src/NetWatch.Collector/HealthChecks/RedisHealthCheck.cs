using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetWatch.Collector.Services;

namespace NetWatch.Collector.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IQueueService _queueService;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IQueueService queueService, ILogger<RedisHealthCheck> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _queueService.IsHealthyAsync(cancellationToken);

            if (isHealthy)
                return HealthCheckResult.Healthy("Redis is responsive");

            _logger.LogWarning("Redis health check failed: Redis is not responsive");
            return HealthCheckResult.Unhealthy("Redis is not responsive");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check error");
            return HealthCheckResult.Unhealthy("Redis health check failed", ex);
        }
    }
}
