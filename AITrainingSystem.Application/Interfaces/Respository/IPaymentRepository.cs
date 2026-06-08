using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByStripeSessionIdAsync(string sessionId);
        Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }
}
