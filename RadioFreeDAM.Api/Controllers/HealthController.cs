using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    // GET api/health
    [HttpGet]
    public ActionResult<HealthDto> Get() =>
        Ok(new HealthDto { Status = "ok", ServerTime = DateTime.UtcNow });
}
