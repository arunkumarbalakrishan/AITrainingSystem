using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Infrastructure.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AITrainingSystem.Infrastructure.Services;

public class CertificatePdfService : ICertificatePdfService
{
    public byte[] GenerateCertificatePdf(
     string studentName,
     string courseTitle,
     string certificateNumber,
     DateTime completionDate,
     string verificationUrl)
    {
        var document = new CertificateDocument(
            studentName,
            courseTitle,
            certificateNumber,
            completionDate,
            verificationUrl);

        return document.GeneratePdf();
    }
}