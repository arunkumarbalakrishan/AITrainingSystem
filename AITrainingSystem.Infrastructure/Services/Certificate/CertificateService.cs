using AITrainingSystem.Application.DTOs.Certificate;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _certificateRepository;

    public CertificateService(
        ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task GenerateCertificateAsync(
        Guid userId,
        Guid courseId)
    {
        var exists = await _certificateRepository
            .ExistsAsync(userId, courseId);

        if (exists)
            return;

        var certificate = new Certificate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            CertificateNumber = GenerateCertificateNumber(),
            IssuedDate = DateTime.UtcNow,
            IsValid = true,
            CreatedAt = DateTime.UtcNow
        };

        await _certificateRepository.AddAsync(certificate);

        await _certificateRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<CertificateDto>>
        GetUserCertificatesAsync(Guid userId)
    {
        var certificates =
            await _certificateRepository
                .GetByUserIdAsync(userId);

        return certificates.Select(c => new CertificateDto
        {
            Id = c.Id,
            CertificateNumber = c.CertificateNumber,
            CourseName = c.Course?.Title ?? string.Empty,
            IssuedDate = c.IssuedDate,
            IsValid = c.IsValid
        });
    }

    public async Task<CertificateDetailsDto?>
        GetCertificateByIdAsync(Guid certificateId)
    {
        var certificate =
            await _certificateRepository
                .GetByIdAsync(certificateId);

        if (certificate == null)
            return null;

        return new CertificateDetailsDto
        {
            Id = certificate.Id,
            CertificateNumber = certificate.CertificateNumber,
            UserName = certificate.User?.FullName ?? string.Empty,
            CourseName = certificate.Course?.Title ?? string.Empty,
            IssuedDate = certificate.IssuedDate,
            IsValid = certificate.IsValid
        };
    }

    public async Task<CertificateVerificationDto?>
        VerifyCertificateAsync(string certificateNumber)
    {
        var certificate =
            await _certificateRepository
                .GetByCertificateNumberAsync(certificateNumber);

        if (certificate == null)
            return null;

        return new CertificateVerificationDto
        {
            CertificateNumber = certificate.CertificateNumber,
            UserName = certificate.User?.FullName ?? string.Empty,
            CourseName = certificate.Course?.Title ?? string.Empty,
            IssuedDate = certificate.IssuedDate,
            IsValid = certificate.IsValid
        };
    }

    private static string GenerateCertificateNumber()
    {
        return $"CERT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}