using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());

            page.Margin(30);

            page.Content()
                .Border(2)
                .Padding(20)
                .Column(column =>
                {
                    column.Spacing(10);

                    column.Item()
                        .AlignCenter()
                        .Text("CERTIFICATE OF COMPLETION")
                        .FontSize(30)
                        .Bold();

                    column.Item()
                        .AlignCenter()
                        .Text("This certificate is proudly presented to")
                        .FontSize(14);

                    column.Item()
                        .AlignCenter()
                        .Text(_studentName)
                        .FontSize(28)
                        .Bold();

                    column.Item()
                        .AlignCenter()
                        .Text("for successfully completing")
                        .FontSize(14);

                    column.Item()
                        .AlignCenter()
                        .Text(_courseTitle)
                        .FontSize(22)
                        .SemiBold();

                    column.Item()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Text($"Completed on {_completionDate:dd MMMM yyyy}")
                        .FontSize(12);

                    column.Item()
                        .PaddingTop(15)
                        .AlignCenter()
                        .Text($"Certificate Number: {_certificateNumber}")
                        .FontSize(12);

                    column.Item()
                        .PaddingTop(20);

                    column.Item()
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Column(signature =>
                                {
                                    signature.Item()
                                        .Text("___________________");

                                    signature.Item()
                                        .Text("Instructor");
                                });

                            row.RelativeItem()
                                .AlignRight()
                                .Column(signature =>
                                {
                                    signature.Item()
                                        .Text("___________________");

                                    signature.Item()
                                        .Text("Director");
                                });
                        });
                });
        });
    }
}