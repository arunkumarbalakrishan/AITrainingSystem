using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace AITrainingSystem.Infrastructure.Documents;

public class CertificateDocument : IDocument
{
    private readonly string _studentName;
    private readonly string _courseTitle;
    private readonly string _certificateNumber;
    private readonly DateTime _completionDate;
    private readonly string _verificationUrl;

    public CertificateDocument(
        string studentName,
        string courseTitle,
        string certificateNumber,
        DateTime completionDate,
        string verificationUrl)
    {
        _studentName = studentName;
        _courseTitle = courseTitle;
        _certificateNumber = certificateNumber;
        _completionDate = completionDate;
        _verificationUrl = verificationUrl;
    }

    public DocumentMetadata GetMetadata()
    {
        return DocumentMetadata.Default;
    }

    public void Compose(IDocumentContainer container)
    {
        var logoPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "logo.png");

        byte[]? logoBytes = null;

        if (File.Exists(logoPath))
        {
            logoBytes = File.ReadAllBytes(logoPath);
        }

        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);

            page.Content()
                .Border(4)
                .Padding(40)
                .Column(column =>
                {
                    column.Spacing(15);

                    // Logo
                    //if (logoBytes != null)
                    //{
                    //    column.Item()
                    //        .AlignCenter()
                    //        .Height(80)
                    //        .Image(logoBytes);
                    //}

                    // LMS Name
                    column.Item()
                        .AlignCenter()
                        .Text("AI TRAINING SYSTEM")
                        .FontSize(22)
                        .Bold();

                    column.Item()
                        .AlignCenter()
                        .Text("Professional Learning Platform")
                        .FontSize(11);

                    // Certificate Title
                    column.Item()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Text("CERTIFICATE OF COMPLETION")
                        .FontSize(34)
                        .Bold();

                    // Description
                    column.Item()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Text("This is to certify that")
                        .FontSize(16);

                    // Student Name
                    column.Item()
                        .AlignCenter()
                        .Text(_studentName.ToUpper())
                        .FontSize(32)
                        .Bold();

                    // Course Text
                    column.Item()
                        .AlignCenter()
                        .Text("has successfully completed the course")
                        .FontSize(15);

                    // Course Name
                    column.Item()
                        .AlignCenter()
                        .Text(_courseTitle.ToUpper())
                        .FontSize(24)
                        .Bold();

                    // Issue Date
                    column.Item()
                        .PaddingTop(15)
                        .AlignCenter()
                        .Text($"Issued On: {_completionDate:dd MMMM yyyy}")
                        .FontSize(12);

                    // Certificate Number
                    column.Item()
                        .AlignCenter()
                        .Text($"Certificate No: {_certificateNumber}")
                        .FontSize(12);

                    // Signature Section
                    column.Item()
                        .PaddingTop(35)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .AlignCenter()
                                .Column(signature =>
                                {
                                    signature.Item()
                                        .Text("________________");

                                    signature.Item()
                                        .Text("Lead Instructor");
                                });

                            row.RelativeItem()
                                .AlignCenter()
                                .Column(signature =>
                                {
                                    signature.Item()
                                        .Text("________________");

                                    signature.Item()
                                        .Text("Program Director");
                                });
                        });

                    // Footer
                    column.Item()
                        .PaddingTop(20)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .AlignLeft()
                                .Text("AI Training System");

                            row.RelativeItem()
                                .AlignRight()
                                .Text("Certificate Verification Available");
                        });
                });
        });
    }
}