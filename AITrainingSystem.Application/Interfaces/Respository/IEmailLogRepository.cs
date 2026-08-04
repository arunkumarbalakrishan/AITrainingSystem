using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories
{
    public interface IEmailLogRepository
    {
        Task AddAsync(EmailLog emailLog);
        Task<EmailLog?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmailLog>> GetPendingOrFailedEmailsAsync(int maxRetries);
        Task UpdateAsync(EmailLog emailLog);
        Task SaveChangesAsync();
    }
}
