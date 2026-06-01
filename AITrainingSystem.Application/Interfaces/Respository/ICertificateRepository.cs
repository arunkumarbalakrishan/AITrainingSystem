using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Interfaces.Respository;

public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(Guid id);

    Task<Certificate?> GetByUserAndCourseAsync(
        Guid userId,
        Guid courseId);

    Task<IEnumerable<Certificate>> GetByUserIdAsync(Guid userId);

    Task<Certificate?> GetByCertificateNumberAsync(
        string certificateNumber);

    Task AddAsync(Certificate certificate);

    Task<bool> ExistsAsync(
        Guid userId,
        Guid courseId);

    Task SaveChangesAsync();
}