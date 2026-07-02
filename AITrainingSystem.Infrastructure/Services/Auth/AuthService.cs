using AITrainingSystem.Application.DTOs.Auth;
using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

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
        var emailNormal = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == emailNormal);
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
        var emailNormal = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == emailNormal);
        if (user == null)
            return false;

        var tokenInput = dto.Token.Trim();
        if (string.IsNullOrEmpty(user.PasswordResetToken) || 
            user.PasswordResetToken.Trim() != tokenInput || 
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

    private static Dictionary<string, object> DecodeJwtPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return new Dictionary<string, object>();

            var payloadBase64 = parts[1];
            payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');
            switch (payloadBase64.Length % 4)
            {
                case 2: payloadBase64 += "=="; break;
                case 3: payloadBase64 += "="; break;
            }

            var bytes = Convert.FromBase64String(payloadBase64);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto)
    {
        try
        {
            var payload = DecodeJwtPayload(dto.IdToken);
            if (payload.Count == 0)
            {
                return new AuthResponseDto
                {
                    Message = "Invalid Google Token format (unable to parse payload)."
                };
            }

            // Verify Issuer (Firebase secure tokens)
            var expectedIssuer = "https://securetoken.google.com/ai-online-training-system";
            if (!payload.TryGetValue("iss", out var issVal) || !string.Equals(issVal?.ToString(), expectedIssuer, StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResponseDto
                {
                    Message = $"Invalid token issuer. Expected: '{expectedIssuer}', Got: '{issVal}'"
                };
            }

            // Verify Audience (Firebase Project ID)
            var expectedAudience = "ai-online-training-system";
            if (!payload.TryGetValue("aud", out var audVal) || !string.Equals(audVal?.ToString(), expectedAudience, StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResponseDto
                {
                    Message = $"Invalid token audience. Expected: '{expectedAudience}', Got: '{audVal}'"
                };
            }

            // Verify Expiration
            if (payload.TryGetValue("exp", out var expVal) && double.TryParse(expVal?.ToString(), out var expSeconds))
            {
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds((long)expSeconds).UtcDateTime;
                if (expDateTime < DateTime.UtcNow)
                {
                    return new AuthResponseDto
                    {
                        Message = $"Google Token expired. Expiration (UTC): {expDateTime}, Current (UTC): {DateTime.UtcNow}"
                    };
                }
            }

            // Extract email
            string? emailClaim = null;
            if (payload.TryGetValue("email", out var emailVal) && emailVal != null)
            {
                emailClaim = emailVal.ToString();
            }
            
            if (string.IsNullOrEmpty(emailClaim) && payload.TryGetValue("identities", out var identitiesVal) && identitiesVal != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(identitiesVal.ToString()!);
                    if (doc.RootElement.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.Array && emailProp.GetArrayLength() > 0)
                    {
                        emailClaim = emailProp[0].GetString();
                    }
                }
                catch { }
            }

            // Extract name
            string? nameClaim = null;
            if (payload.TryGetValue("name", out var nameVal) && nameVal != null)
            {
                nameClaim = nameVal.ToString();
            }
            if (string.IsNullOrEmpty(nameClaim))
            {
                nameClaim = emailClaim?.Split('@')[0] ?? "Google User";
            }

            if (string.IsNullOrEmpty(emailClaim))
            {
                return new AuthResponseDto
                {
                    Message = $"Email claim missing from Google Token. Raw payload: {JsonSerializer.Serialize(payload)}"
                };
            }

            // Get or create user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = nameClaim,
                    Email = emailClaim,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password since login is via Google
                    Role = "Student",
                    IsApprovedTrainer = true
                };
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
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
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Message = $"Google Login verification failed: {ex.Message} | StackTrace: {ex.StackTrace}"
            };
        }
    }
}