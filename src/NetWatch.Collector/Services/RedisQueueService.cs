
using StackExchange.Redis;
using System.Text.Json;

namespace NetWatch.Collector.Services;

public class RedisQueueService : IQueueService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisQueueService> _logger;
    private bool _disposed;

    public RedisQueueService(IConnectionMultiplexer redis, ILogger<RedisQueueService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("RedisQueueService initialized.");
    }

    public async Task<string> EnqueueAsync<T>(string queueName, T item, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisQueueService));

        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException(nameof(queueName));

        if (item == null)
            throw new ArgumentNullException(nameof(item));

        try
        {
            var db = _redis.GetDatabase();

            var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var streamId = await db.StreamAddAsync(
                queueName,
                new NameValueEntry[]
                {
                    new("data", json),
                    new("enqueuedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()),
                    new("type", typeof(T).Name)
                });

            _logger.LogDebug($"Enqueued item to {queueName}. StreamId: {streamId}, Type: {typeof(T).Name}");

            return streamId.ToString();
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, $"Redis error enqueuing to {queueName}");
            throw new InvalidOperationException($"Failed to enqueue to {queueName}", ex);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisQueueService));

        try
        {
            var db = _redis.GetDatabase();
            var pingResult = await db.PingAsync();

            _logger.LogTrace($"Redis health check: {pingResult.TotalMilliseconds}ms");

            return pingResult.TotalMilliseconds < 1000;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed");
            return false;
        }
    }

    public async Task<QueueStats> GetStatsAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisQueueService));

        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(queueName));

        try
        {
            var db = _redis.GetDatabase();

            var exists = await db.KeyExistsAsync(queueName);
            if (!exists)
            {
                return new QueueStats(
                        queueName,
                        0,
                        0,
                        DateTimeOffset.MinValue
                );
            }

            var streamInfo = await db.StreamInfoAsync(queueName);

            var lastEnqueuedAt = DateTimeOffset.MinValue;

            if (streamInfo.Length > 0)
            {
                var allMessages = await db.StreamRangeAsync(queueName, "-", "+");

                if (allMessages.Length > 0)
                {
                    var lastMessage = allMessages[allMessages.Length - 1];

                    foreach (var field in lastMessage.Values)
                    {
                        if (field.Name == "enqueued_at")
                        {
                            if (long.TryParse(field.Value, out var ms))
                            {
                                lastEnqueuedAt = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                            }
                            break;
                        }
                    }
                }
            }

            _logger.LogDebug($"Queue stats: {queueName} - Pending: {streamInfo.Length}, LastEnqueued: {lastEnqueuedAt}");

            return new QueueStats(
                queueName,
                streamInfo.Length,
                0,
                lastEnqueuedAt
            );
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, $"Redis error getting stats for {queueName}");
            throw new InvalidOperationException($"Failed to get stats for {queueName}", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logger.LogInformation("RedisQueueService disposed.");
    }
}
