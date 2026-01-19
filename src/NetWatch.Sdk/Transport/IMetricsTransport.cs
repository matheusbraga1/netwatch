using NetWatch.Shared.Models;

namespace NetWatch.Sdk.Transport;

public interface IMetricsTransport
{
    Task<bool> SendBatchAsync(IReadOnlyList<RequestMetric> metrics, CancellationToken cancellationToken);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
