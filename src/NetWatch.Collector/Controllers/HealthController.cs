using Microsoft.AspNetCore.Mvc;

namespace NetWatch.Collector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new 
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0"
        });
    }
}
