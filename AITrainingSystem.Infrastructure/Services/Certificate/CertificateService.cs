using AITrainingSystem.Application.DTOs.Certificate;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificatePdfService _certificatePdfService;
    private readonly IUserRepository _userRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly INotificationService _notificationService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public CertificateService(
        ICertificateRepository certificateRepository, 
        ICertificatePdfService certificatePdfService,
        IUserRepository userRepository,
        ICourseRepository courseRepository,
        INotificationService notificationService,
        IEmailTemplateService emailTemplateService,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _certificateRepository = certificateRepository;
        _certificatePdfService = certificatePdfService;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _notificationService = notificationService;
        _emailTemplateService = emailTemplateService;
        _configuration = configuration;
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

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            var course = await _courseRepository.GetByIdAsync(courseId);

            if (user != null && course != null)
            {
                await _notificationService.CreateInAppNotificationAsync(
                    userId,
                    "Certificate Issued",
                    $"Congratulations! Your certificate for '{course.Title}' has been successfully issued."
                );

                var clientUrl = _configuration["ClientUrl"] ?? "http://localhost:4200";
                var placeholders = new Dictionary<string, string>
                {
                    { "StudentName", user.FullName },
                    { "CourseName", course.Title },
                    { "CompletionDate", certificate.IssuedDate.ToString("MMMM dd, yyyy") },
                    { "Score", "100%" },
                    { "CertificateId", certificate.CertificateNumber },
                    { "ActionButtonText", "View Certificate" },
                    { "ActionButtonUrl", $"{clientUrl.TrimEnd('/')}/profile/certificates" },
                    { "SupportEmail", "support@aitrainingsystem.com" },
                    { "SupportPhone", "+1 (555) 019-2834" }
                };

                await _emailTemplateService.QueueEmailAsync(user.Email, $"Certificate Awarded: {course.Title}!", "CertificateIssued", placeholders);
            }
        }
        catch (Exception)
        {
            // Logging is optional, we want to fail-safe so certificate creation succeeds even if SMTP fails
        }
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

    public async Task<byte[]> DownloadCertificatePdfAsync(
    Guid certificateId)
    {
        var certificate =
            await _certificateRepository
                .GetCertificateWithDetailsAsync(certificateId);

        if (certificate == null)
            throw new Exception("Certificate not found");

        var verificationUrl =
            $"https://your-domain.com/api/certificate/verify/{certificate.CertificateNumber}";

        return _certificatePdfService.GenerateCertificatePdf(
            certificate.User.FullName,
            certificate.Course.Title,
            certificate.CertificateNumber,
            certificate.IssuedDate,
            verificationUrl);
    }

    private static string GenerateCertificateNumber()
    {
        return $"CERT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}