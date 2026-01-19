namespace NetWatch.Sdk.Models;

public class MetricsBatchRequest
{
    public List<RequestMetric> Metrics { get; set; } = new();
    public string SdkVersion { get; set; } = "1.0.0";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Hostname { get; set; }
}
