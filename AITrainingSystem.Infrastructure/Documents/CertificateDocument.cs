using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AITrainingSystem.Infrastructure.Documents
{
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

            var bgPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "certificate_bg.png");

            byte[]? logoBytes = null;
            if (File.Exists(logoPath))
            {
                logoBytes = File.ReadAllBytes(logoPath);
            }

            byte[]? bgBytes = null;
            if (File.Exists(bgPath))
            {
                bgBytes = File.ReadAllBytes(bgPath);
            }

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0); // Zero margin so the background stretches completely

                page.Content().Layers(layers =>
                {
                    // Layer 1: Background Template Image
                    if (bgBytes != null)
                    {
                        layers.Layer().Image(bgBytes);
                    }
                    else
                    {
                        // Fallback background color if image not found
                        layers.Layer().Background(Colors.Grey.Lighten4);
                    }

                    // Layer 2: Certificate Typography Content
                    layers.PrimaryLayer()
                        .Padding(50)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // Top Header: logo & LMS branding
                            column.Item().Row(row =>
                            {
                                if (logoBytes != null)
                                {
                                    row.AutoItem().Height(40).Image(logoBytes);
                                    row.AutoItem().PaddingLeft(10).PaddingRight(10).AlignMiddle().Text("|").FontSize(20).FontColor(Colors.Grey.Lighten1);
                                }
                                
                                row.RelativeItem().AlignMiddle().Column(headerCol =>
                                {
                                    headerCol.Item().Text("AI TRAINING SYSTEM").FontSize(14).Bold().FontColor("#0B4F6C");
                                    headerCol.Item().Text("Training Services").FontSize(10).FontColor(Colors.Grey.Darken2);
                                });
                            });

                            // Main body (relative widths prevent overlap with the right side graphics)
                            column.Item().PaddingTop(25).Row(row =>
                            {
                                row.RelativeItem(7).Column(textCol =>
                                {
                                    textCol.Spacing(15);

                                    textCol.Item().Text("Course Completion Certificate")
                                        .FontSize(24)
                                        .FontColor("#0B4F6C")
                                        .Medium();

                                    textCol.Item().PaddingTop(5).Text(_studentName)
                                        .FontSize(32)
                                        .Bold()
                                        .FontColor(Colors.Black);

                                    textCol.Item().Text("has successfully completed the self-paced training course")
                                        .FontSize(13)
                                        .FontColor(Colors.Grey.Darken2);

                                    textCol.Item().Text(_courseTitle)
                                        .FontSize(26)
                                        .Bold()
                                        .FontColor("#1A5F7A");

                                    // Signature block
                                    textCol.Item().PaddingTop(35).Row(sigRow =>
                                    {
                                        sigRow.RelativeItem().Column(sigCol =>
                                        {
                                            sigCol.Item().Width(150).Height(30).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                            sigCol.Item().PaddingTop(5).Text("DIRECTOR, TRAINING SERVICES").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                                        });

                                        sigRow.RelativeItem().Column(dateCol =>
                                        {
                                            dateCol.Item().PaddingTop(20).Text(_completionDate.ToString("dd MMMM yyyy")).FontSize(12).FontColor(Colors.Black);
                                        });
                                    });
                                });

                                // Margin spacer to push text leftward of background graphic
                                row.RelativeItem(3);
                            });

                            // Footer: ID & Verification link at the bottom
                            column.Item().AlignBottom().Row(footerRow =>
                            {
                                footerRow.RelativeItem().Text($"Certificate ID: {_certificateNumber}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                footerRow.RelativeItem().AlignRight().Text($"Verification: {_verificationUrl}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });
                });
            });
        }
    }
}