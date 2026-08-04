using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Persistence.Repositories
{
    public class EmailLogRepository : IEmailLogRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EmailLog emailLog)
        {
            await _context.EmailLogs.AddAsync(emailLog);
        }

        public async Task<EmailLog?> GetByIdAsync(Guid id)
        {
            return await _context.EmailLogs.FindAsync(id);
        }

        public async Task<IEnumerable<EmailLog>> GetPendingOrFailedEmailsAsync(int maxRetries)
        {
            return await _context.EmailLogs
                .Where(e => (e.Status == "Pending" || e.Status == "Failed") && e.RetryCount < maxRetries)
                .OrderBy(e => e.CreatedTime)
                .Take(20) // Process in chunks of 20
                .ToListAsync();
        }

        public async Task UpdateAsync(EmailLog emailLog)
        {
            _context.EmailLogs.Update(emailLog);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
