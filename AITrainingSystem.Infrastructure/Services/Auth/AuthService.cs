using AITrainingSystem.Application.DTOs.Auth;
using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtService _jwtService;
    private readonly INotificationService _notificationService;

    public AuthService(ApplicationDbContext context, JwtService jwtService, INotificationService notificationService)
    {
        _context = context;
        _jwtService = jwtService;
        _notificationService = notificationService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Message = "Email already exists"
            };
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsApprovedTrainer = !request.Role.Equals("Trainer", StringComparison.OrdinalIgnoreCase)
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            Message = "User registered successfully"
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Message = "Invalid email or password"
            };
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash
        );

        if (!isPasswordValid)
        {
            return new AuthResponseDto
            {
                Message = "Invalid email or password"
            };
        }

        var accessToken = _jwtService.GenerateToken(user);

        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            Role = user.Role,
            Message = "Login successful"
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(
    RefreshTokenRequestDto dto)
    {
        var existingToken = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Token == dto.RefreshToken);

        if (existingToken == null)
            throw new Exception("Invalid refresh token");

        if (existingToken.IsRevoked)
            throw new Exception("Refresh token revoked");

        if (existingToken.ExpiryDate < DateTime.UtcNow)
            throw new Exception("Refresh token expired");

        var newAccessToken =
            _jwtService.GenerateToken(existingToken.User);

        var newRefreshToken =
            _jwtService.GenerateRefreshToken();

        existingToken.Token = newRefreshToken;
        existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            Email = existingToken.User.Email,
            Role = existingToken.User.Role,
            Message = "Token refreshed successfully"
        };
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
        if (user == null)
        {
            return false;
        }

        var random = new Random();
        var token = random.Next(100000, 999999).ToString();

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        var subject = "AITrainingSystem Password Reset Request";
        var body = $"<h3>Password Reset Request</h3><p>Your password reset code is: <strong>{token}</strong></p><p>This code will expire in 1 hour.</p>";
        await _notificationService.SendEmailAsync(user.Email, subject, body);

        Console.WriteLine($"[PASSWORD_RESET_TOKEN] Email: {user.Email}, Token: {token}");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
        if (user == null)
            return false;

        if (string.IsNullOrEmpty(user.PasswordResetToken) || 
            user.PasswordResetToken != dto.Token || 
            !user.PasswordResetTokenExpiry.HasValue || 
            user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();
        return true;
    }
}