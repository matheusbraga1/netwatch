namespace NetWatch.Sdk.Models;

public class MetricsBatchResponse
{
    public Guid JobId { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
}
