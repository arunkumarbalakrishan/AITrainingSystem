using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IEnrollmentService
    {
        Task EnrollUserAsync(Guid userId, Guid courseId);
    }
}
