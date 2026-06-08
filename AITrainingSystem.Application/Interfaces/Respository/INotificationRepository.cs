using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification?> GetByIdAsync(Guid id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
