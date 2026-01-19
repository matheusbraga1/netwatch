namespace NetWatch.Collector.Models;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public List<string>? ValidationErrors { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
