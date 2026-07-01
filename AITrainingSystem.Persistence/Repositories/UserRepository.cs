using AITrainingSystem.Application.DTOs.Common;
using AITrainingSystem.Application.DTOs.Users;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Persistence.Repositories;

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<User>> GetAllUsersAsync(UserQueryParams queryParams)
    {
        var query = _context.Users.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            query = query.Where(x =>
                x.FullName.Contains(queryParams.Search) ||
                x.Email.Contains(queryParams.Search));
        }

        // Filter by Role
        if (!string.IsNullOrWhiteSpace(queryParams.Role))
        {
            query = query.Where(x => x.Role == queryParams.Role);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = users,
            TotalCount = totalCount,
            CurrentPage = queryParams.PageNumber,
            PageSize = queryParams.PageSize
        };
    }


}

