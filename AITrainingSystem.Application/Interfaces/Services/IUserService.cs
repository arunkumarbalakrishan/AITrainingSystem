using AITrainingSystem.Application.DTOs.Common;
using AITrainingSystem.Application.DTOs.User;
using AITrainingSystem.Application.DTOs.Users;

namespace AITrainingSystem.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResponseDto?> GetCurrentUserAsync(Guid userId);
    Task<List<UserResponseDto>> GetAllUsersAsync();
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);

    Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(Guid id);
    Task<PagedResult<UserDto>> GetAllUsersAsync(UserQueryParams queryParams);
}