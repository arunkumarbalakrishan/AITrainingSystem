using AITrainingSystem.Application.DTOs.Common;
using AITrainingSystem.Application.DTOs.User;
using AITrainingSystem.Application.DTOs.Users;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;

namespace AITrainingSystem.Infrastructure.Services;

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
            Role = user.Role
        };
    }
    public async Task<List<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();

        return users.Select(user => new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        }).ToList();
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
            Role = user.Role
        };
    }
    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return false;

        user.FullName = dto.FullName;
        user.Role = dto.Role;

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
    public async Task<PagedResult<UserDto>> GetAllUsersAsync(UserQueryParams queryParams)
    {
        var pagedUsers = await _userRepository.GetAllUsersAsync(queryParams);

        return new PagedResult<UserDto>
        {
            Items = pagedUsers.Items.Select(x => new UserDto
            {
                Id = x.Id,
                Name = x.FullName,
                Email = x.Email,
                Role = x.Role
            }),
            TotalCount = pagedUsers.TotalCount,
            CurrentPage = pagedUsers.CurrentPage,
            PageSize = pagedUsers.PageSize
        };
    }

}