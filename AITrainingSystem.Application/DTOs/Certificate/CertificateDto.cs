namespace AITrainingSystem.Application.DTOs.Certificate;

public class CertificateDto
{
    public Guid Id { get; set; }

    public string CertificateNumber { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public DateTime IssuedDate { get; set; }

    public bool IsValid { get; set; }
}