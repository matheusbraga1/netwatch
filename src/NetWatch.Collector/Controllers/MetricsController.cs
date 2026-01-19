using Microsoft.AspNetCore.Mvc;
using NetWatch.Collector.Models;
using NetWatch.Collector.Services;
using NetWatch.Shared.Models;

namespace NetWatch.Collector.Controllers;


[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ILogger<MetricsController> _logger;
    private readonly IQueueService _queueService;

    public MetricsController(ILogger<MetricsController> logger, IQueueService queueService)
    {
        _logger = logger;
        _queueService = queueService;
    }

    [HttpPost("batch")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MetricsBatchResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ReceiveBatch([FromBody] MetricsBatchRequest request)
    {
        if (request == null)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid request",
                Detail = "Request body is null or invalid",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (request.Metrics == null || request.Metrics.Count == 0)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Invalid request",
                Detail = "Metrics list is empty",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (request.Metrics.Count > 1000)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Batch too large",
                Detail = $"Maximum batch size is 1000, received {request.Metrics.Count}",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation($"Received batch with {request.Metrics.Count} metrics from {request.Hostname ?? "unknown"}");

        try
        {
            var job = new MetricsProcessingJob
            {
                JobId = Guid.NewGuid(),
                ProjectId = Guid.Empty,
                Metrics = request.Metrics,
                ReceivedAt = DateTimeOffset.UtcNow,
                Source = request.Hostname,
                SdkVersion = request.SdkVersion
            };

            var streamId = await _queueService.EnqueueAsync("metrics:pending", job);

            _logger.LogInformation(
                $"Batch enqueued. JobId: {job.JobId}, StreamId: {streamId}, Metrics: {request.Metrics.Count}");

            return Accepted(new MetricsBatchResponse
            {
                JobId = job.JobId,
                Accepted = request.Metrics.Count,
                Rejected = 0,
                Message = "Batch queued for processing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue batch");

            return StatusCode(503, new ErrorResponse
            {
                Error = "Service unavailable",
                Detail = "Failed to queue metrics for processing. Redis might be unavailable.",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }

    [HttpGet("queue/stats")]
    [ProducesResponseType(typeof(QueueStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueStats()
    {
        try
        {
            var stats = await _queueService.GetStatsAsync("metrics:pending");
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get queue stats");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal Server Error",
                Detail = "Failed to retrieve queue stats",
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
