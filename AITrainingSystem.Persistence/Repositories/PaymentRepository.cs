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
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            return payment;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _context.Payments.FindAsync(id);
        }

        public async Task<Payment?> GetByStripeSessionIdAsync(string sessionId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.StripeSessionId == sessionId);
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
