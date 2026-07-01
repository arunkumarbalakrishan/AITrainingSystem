using AITrainingSystem.Application.DTOs.Common;
using AITrainingSystem.Application.DTOs.Users;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByEmailAsync(string email);

    Task<List<User>> GetAllAsync();

    Task AddAsync(User user);

    Task UpdateAsync(User user);

    Task DeleteAsync(User user);
    Task<PagedResult<User>> GetAllUsersAsync(UserQueryParams queryParams);

}

