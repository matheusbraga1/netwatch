using Microsoft.AspNetCore.Mvc;
using NetWatch.Collector.Models;
using NetWatch.Shared.Models;

namespace NetWatch.Collector.Controllers;


[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(ILogger<MetricsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("batch")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MetricsBatchResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveBatch([FromBody] MetricsBatchRequest request)
    {
        if (request == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Invalid request",
                Detail = "Request body is null or invalid",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (request.Metrics == null || request.Metrics.Count == 0)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Invalid request",
                Detail = "Metrics list is empty",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        if (request.Metrics.Count > 1000)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Batch too large",
                Detail = $"Maximum batch size is 1000, received {request.Metrics.Count}",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation($"Received batch with {request.Metrics.Count} metrics from {request.Hostname ?? "unknown"}");

        await Task.Delay(10);

        var jobId = Guid.NewGuid();

        _logger.LogInformation($"Batch accepted. JobId: {jobId}, Metrics: {request.Metrics.Count}");

        return Accepted(new MetricsBatchResponse
        {
            JobId = jobId,
            Accepted = request.Metrics.Count,
            Rejected = 0,
            Message = "Batch queued for processing"
        });
    }
}
