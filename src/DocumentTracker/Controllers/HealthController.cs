using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            Status = "Healthy",
            TimestampUtc = DateTime.UtcNow
        });
    }
}
