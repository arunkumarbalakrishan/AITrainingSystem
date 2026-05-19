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
}