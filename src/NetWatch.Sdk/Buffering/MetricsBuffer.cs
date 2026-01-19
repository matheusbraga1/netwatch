using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetWatch.Sdk.Configuration;
using NetWatch.Shared.Models;
using NetWatch.Sdk.Transport;
using System.Collections.Concurrent;

namespace NetWatch.Sdk.Buffering;

public class MetricsBuffer : IMetricsBuffer, IDisposable
{
    private readonly ConcurrentQueue<RequestMetric> _queue = new();
    private readonly NetWatchOptions _options;
    private readonly ILogger<MetricsBuffer> _logger;
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly IMetricsTransport _transport;
    private int _queueSize = 0;
    private bool _disposed = false;

    public MetricsBuffer(
        IMetricsTransport transport,
        IOptions<NetWatchOptions> options,
        ILogger<MetricsBuffer> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _flushTimer = new Timer(
            callback: async _ => await FlushAsync(),
            state: null,
            dueTime: TimeSpan.FromSeconds(_options.FlushIntervalSeconds),
            period: TimeSpan.FromSeconds(_options.FlushIntervalSeconds)
        );

        _logger.LogInformation(
            $"MetricBuffer initialized with FlushInterval={_options.FlushIntervalSeconds}, MaxBufferSize={_options.MaxBufferSize}");
    }

    public int Count => _queueSize;

    public async Task AddAsync(RequestMetric metric)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MetricsBuffer));

        if (metric == null)
            throw new ArgumentNullException(nameof(metric));

        _queue.Enqueue(metric);
        var currentSize = Interlocked.Increment(ref _queueSize);

        _logger.LogTrace($"Metric added to buffer. Queue size: {currentSize}");

        if (currentSize >= _options.MaxBufferSize)
        {
            _logger.LogDebug($"Buffer reached max size ({_options.MaxBufferSize}). Triggering flush.");
            _ = Task.Run(async () => await FlushAsync());
        }

        await Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        if (_disposed)
            return;

        if (!await _flushLock.WaitAsync(0))
        {
            _logger.LogTrace("Flush already in progress. Skipping this flush call.");
            return;
        }

        try
        {
            if (_queue.IsEmpty)
            {
                _logger.LogTrace("Buffer is empty, nothing to flush");
                return;
            }

            var batch = new List<RequestMetric>();
            while (_queue.TryDequeue(out var metric) && batch.Count < _options.MaxBufferSize)
            {
                batch.Add(metric);
                Interlocked.Decrement(ref _queueSize);
            }

            if (batch.Count == 0)
                return;

            _logger.LogInformation($"Flushing {batch.Count} metrics to collector.");

            var success = await _transport.SendBatchAsync(batch, CancellationToken.None);

            if (success)
            {
                _logger.LogInformation($"Successfully flushed {batch.Count} metrics");
            }
            else
            {
                _logger.LogWarning($"Failed to flush {batch.Count} metrics");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while flushing metrics.");
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogInformation("Disposing MetricBuffer. Flushing remaining metrics...");

        _flushTimer?.Dispose();

        try
        {
            FlushAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final flush");
        }

        _flushLock?.Dispose();

        _logger.LogInformation("MetricBuffer disposed.");
    }
}
