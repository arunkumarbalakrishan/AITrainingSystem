using AITrainingSystem.Application.DTOs.Certificate;

namespace AITrainingSystem.Application.Interfaces.Services;

public interface ICertificateService
{
    Task GenerateCertificateAsync(
        Guid userId,
        Guid courseId);

    Task<IEnumerable<CertificateDto>> GetUserCertificatesAsync(
        Guid userId);

    Task<CertificateDetailsDto?> GetCertificateByIdAsync(
        Guid certificateId);

    Task<CertificateVerificationDto?> VerifyCertificateAsync(
        string certificateNumber);
}