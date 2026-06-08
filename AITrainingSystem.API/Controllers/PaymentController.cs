using AITrainingSystem.Application.DTOs.Payment;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _paymentService.CreateCheckoutSessionAsync(userId, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

        var result = await _paymentService.ProcessWebhookAsync(json, signatureHeader);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmMockPayment([FromQuery] Guid courseId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _paymentService.ConfirmMockPaymentAsync(userId, courseId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
