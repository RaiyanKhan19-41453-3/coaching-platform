using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachingPlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class InfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        service = "Coaching Platform API",
        status = "running",
        architecture = "Controllers -> Services -> MediatR handlers -> EF Core"
    });
}
