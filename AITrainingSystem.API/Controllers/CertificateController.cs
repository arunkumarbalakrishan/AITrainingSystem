using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CertificateController : ControllerBase
{
    private readonly ICertificateService _certificateService;

    public CertificateController(
        ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpGet("my-certificates")]
    public async Task<IActionResult> GetMyCertificates()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var certificates =
            await _certificateService
                .GetUserCertificatesAsync(userId);

        return Ok(certificates);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCertificate(Guid id)
    {
        var certificate =
            await _certificateService
                .GetCertificateByIdAsync(id);

        if (certificate == null)
            return NotFound();

        return Ok(certificate);
    }

    [AllowAnonymous]
    [HttpGet("verify/{certificateNumber}")]
    public async Task<IActionResult> VerifyCertificate(
        string certificateNumber)
    {
        var certificate =
            await _certificateService
                .VerifyCertificateAsync(certificateNumber);

        if (certificate == null)
            return NotFound(new
            {
                Message = "Certificate not found"
            });

        return Ok(certificate);
    }
}