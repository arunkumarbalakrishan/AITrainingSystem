using AITrainingSystem.Application.DTOs.Common;
using AITrainingSystem.Application.DTOs.User;
using AITrainingSystem.Application.DTOs.Users;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Infrastructure.Services.users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponseDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            return null;

        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsApprovedTrainer = user.IsApprovedTrainer
        };
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return null;

        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsApprovedTrainer = user.IsApprovedTrainer
        };
    }
    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return false;

        user.FullName = dto.FullName;
        user.Role = dto.Role;
        if (dto.IsApprovedTrainer.HasValue)
        {
            user.IsApprovedTrainer = dto.IsApprovedTrainer.Value;
        }

        await _userRepository.UpdateAsync(user);

        return true;
    }
    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return false;

        await _userRepository.DeleteAsync(user);

        return true;
    }
    public async Task<PagedResult<UserResponseDto>> GetAllUsersAsync(UserQueryParams queryParams)
    {
        var pagedUsers = await _userRepository.GetAllUsersAsync(queryParams);

        return new PagedResult<UserResponseDto>
        {
            Items = pagedUsers.Items.Select(x => new UserResponseDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role,
                IsApprovedTrainer = x.IsApprovedTrainer
            }),
            TotalCount = pagedUsers.TotalCount,
            CurrentPage = pagedUsers.CurrentPage,
            PageSize = pagedUsers.PageSize
        };
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        user.FullName = dto.FullName;

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword) || 
                !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                throw new System.Exception("Current password verification failed.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        }

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<UserResponseDto?> CreateUserAsync(CreateUserDto dto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            throw new System.Exception("Email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            IsApprovedTrainer = !dto.Role.Equals("Trainer", StringComparison.OrdinalIgnoreCase)
        };

        await _userRepository.AddAsync(user);

        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsApprovedTrainer = user.IsApprovedTrainer
        };
    }
}