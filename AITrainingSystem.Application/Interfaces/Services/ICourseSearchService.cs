using AITrainingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface ICourseSearchService
    {
        Task IndexCourseAsync(Course course);
        Task DeleteCourseAsync(Guid courseId);
        Task<IEnumerable<Course>> SearchCoursesAsync(string query);
    }
}
