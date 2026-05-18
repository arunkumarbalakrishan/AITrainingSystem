using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("Admin")]
    public IActionResult GetProfile()
    {
        return Ok("Protected API Working");
    }
}