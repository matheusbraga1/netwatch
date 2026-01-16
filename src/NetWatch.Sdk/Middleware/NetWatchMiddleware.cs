using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetWatch.Sdk.Configuration;
using NetWatch.Sdk.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace NetWatch.Sdk.Middleware;

public class NetWatchMiddleware
{
    private readonly RequestDelegate _next;
    private readonly NetWatchOptions _options;
    private readonly ILogger<NetWatchMiddleware> _logger;

    public NetWatchMiddleware(
        RequestDelegate next,
        IOptions<NetWatchOptions> options,
        ILogger<NetWatchMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldIgnorePath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!ShouldSample())
        {
            await _next(context);
            return;
        }

        var traceId = GenerateTraceId();
        var stopWatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-NetWatch-TraceId"))
            {
                context.Response.Headers["X-NetWatch-TraceId"] = traceId;
            }
            return Task.CompletedTask;
        });

        Exception? capturedException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            stopWatch.Stop();

            var metric = CreateMetric(
                context,
                traceId,
                stopWatch.ElapsedMilliseconds,
                capturedException);

            _logger.LogDebug(
                $"Captured metric: {metric.Method} {metric.Path} - {metric.DurationMs}ms - Status: {metric.StatusCode}");
        }
    }

    private bool ShouldIgnorePath(PathString path)
    {
        return _options.IgnorePaths.Any(ignorePath => 
            path.StartsWithSegments(ignorePath, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldSample()
    {
        if (_options.SampleRate >= 1.0)
            return true;

        if (_options.SampleRate <= 0.0)
            return false;

        return Random.Shared.NextDouble() < _options.SampleRate;
    }

    private static string GenerateTraceId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"nw_{timestamp}_{random}";
    }

    private RequestMetric CreateMetric(
        HttpContext context,
        string traceId,
        long durationMs,
        Exception? exception)
    {
        return new RequestMetric
        {
            TraceId = traceId,
            Timestamp = DateTimeOffset.UtcNow,
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? "/",
            QueryString = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value 
                : null,
            StatusCode = context.Response.StatusCode,
            DurationMs = durationMs,

            UserId = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null,
            UserName = context.User?.Identity?.Name,

            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers["User-Agent"].ToString(),

            ExceptionType = exception?.GetType().Name,
            ExceptionMessage = exception?.Message,
            HasException = exception != null
        };
    }
}
