using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories;

public interface ICourseRepository
{
    Task<Course> CreateAsync(Course course);

    Task<IEnumerable<Course>> GetAllAsync();

    Task<Course?> GetByIdAsync(Guid id);

    Task UpdateAsync(Course course);

    Task DeleteAsync(Course course);

    Task<bool> ExistsAsync(Guid id);
}
