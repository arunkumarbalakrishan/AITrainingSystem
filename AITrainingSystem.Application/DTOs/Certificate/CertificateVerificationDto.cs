namespace AITrainingSystem.Application.DTOs.Certificate;

public class CertificateVerificationDto
{
    public string CertificateNumber { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public DateTime IssuedDate { get; set; }

    public bool IsValid { get; set; }
}