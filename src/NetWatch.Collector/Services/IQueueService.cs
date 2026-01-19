namespace NetWatch.Collector.Services;

public interface IQueueService
{
    Task<string> EnqueueAsync<T>(string queueName, T item, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<QueueStats> GetStatsAsync(string queueName, CancellationToken cancellationToken = default);
}

public record QueueStats(
    string QueueName,
    int PendingCount,
    int ProcessedCount,
    DateTimeOffset LastEnqueueAt
);
