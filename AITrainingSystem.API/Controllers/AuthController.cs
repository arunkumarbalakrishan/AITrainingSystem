using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Auth;
using AITrainingSystem.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "User registered successfully"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            return Unauthorized(new
            {
                message = result.Message
            });
        }

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful"));
    }
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(
    RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            return Unauthorized(new
            {
                message = result.Message
            });
        }

        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Token refreshed successfully"));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var sent = await _authService.ForgotPasswordAsync(dto);
        if (!sent)
        {
            return BadRequest(ApiResponse<object>.FailResponse("Email not found."));
        }
        return Ok(ApiResponse<object>.SuccessResponse(null, "Password reset code sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        var reset = await _authService.ResetPasswordAsync(dto);
        if (!reset)
        {
            return BadRequest(ApiResponse<object>.FailResponse("Invalid reset token, or token expired."));
        }
        return Ok(ApiResponse<object>.SuccessResponse(null, "Password has been reset successfully."));
    }
}