using NetWatch.Shared.Models;

namespace NetWatch.Sdk.Buffering;

public interface IMetricsBuffer
{
    Task AddAsync(RequestMetric metric);
    Task FlushAsync();
    int Count { get; }
}
