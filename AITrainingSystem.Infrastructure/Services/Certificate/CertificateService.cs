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

    public CertificateService(
        ICertificateRepository certificateRepository, 
        ICertificatePdfService certificatePdfService,
        IUserRepository userRepository,
        ICourseRepository courseRepository,
        INotificationService notificationService)
    {
        _certificateRepository = certificateRepository;
        _certificatePdfService = certificatePdfService;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _notificationService = notificationService;
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

                var subject = $"Certificate Awarded: {course.Title}!";
                var body = $"<h3>Congratulations {user.FullName}!</h3>" +
                           $"<p>You have successfully completed all requirements for the course <strong>{course.Title}</strong>.</p>" +
                           $"<p>Your official Certificate of Completion has been generated.</p>" +
                           $"<p><strong>Certificate Number:</strong> {certificate.CertificateNumber}</p>" +
                           $"<p><strong>Issued Date:</strong> {certificate.IssuedDate:MMMM dd, yyyy}</p>" +
                           $"<br/><p>You can download your PDF certificate directly from your AITraining dashboard under 'Certificates'.</p>" +
                           $"<p>Best regards,<br/>AITraining System</p>";

                await _notificationService.SendEmailAsync(user.Email, subject, body);
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