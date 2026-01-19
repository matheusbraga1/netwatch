namespace NetWatch.Shared.Models;

public class MetricsProcessingJob
{
    public Guid JobId { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public List<RequestMetric> Metrics { get; set; } = new();
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Source { get; set; }
    public string? SdkVersion { get; set; }
}
