namespace AITrainingSystem.Application.Interfaces.Services;

public interface ICertificatePdfService
{
    byte[] GenerateCertificatePdf(
        string studentName,
        string courseTitle,
        string certificateNumber,
        DateTime completionDate,
        string verificationUrl);
}