namespace NetWatch.Sdk.Configuration;

public class NetWatchOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string CollectorEndpoint { get; set; } = string.Empty;
    public int FlushIntervalSeconds { get; set; } = 5;
    public int MaxBufferSize { get; set; } = 1000;
    public double SampleRate { get; set; } = 1.0;
    public List<string> IgnorePaths { get; set; } = new()
    {
        "/health",
        "/healthz",
        "/ready",
        "/alive"
    };

    public List<string> CapturedHeaders { get; set; } = new()
    {
        "User-Agent"
    };
}
