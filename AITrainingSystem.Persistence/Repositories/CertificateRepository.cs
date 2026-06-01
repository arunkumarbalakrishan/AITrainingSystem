using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Persistence.Repositories;

public class CertificateRepository : ICertificateRepository
{
    private readonly ApplicationDbContext _context;

    public CertificateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Certificate?> GetByIdAsync(Guid id)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Certificate?> GetByUserAndCourseAsync(
        Guid userId,
        Guid courseId)
    {
        return await _context.Certificates
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.CourseId == courseId);
    }

    public async Task<IEnumerable<Certificate>> GetByUserIdAsync(
        Guid userId)
    {
        return await _context.Certificates
            .Include(c => c.Course)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedDate)
            .ToListAsync();
    }

    public async Task<Certificate?> GetByCertificateNumberAsync(
        string certificateNumber)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c =>
                c.CertificateNumber == certificateNumber);
    }

    public async Task AddAsync(Certificate certificate)
    {
        await _context.Certificates.AddAsync(certificate);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid courseId)
    {
        return await _context.Certificates
            .AnyAsync(c =>
                c.UserId == userId &&
                c.CourseId == courseId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}