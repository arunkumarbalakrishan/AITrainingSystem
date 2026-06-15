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
            var bgPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "certificate_bg.png");

            var logoPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "logo.png");

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
                page.Margin(0);

                page.Content().Layers(layers =>
                {
                    // Background Layer (User's border image)
                    if (bgBytes != null)
                    {
                        layers.Layer().Image(bgBytes).FitArea();
                    }
                    else
                    {
                        // Fallback classic border if the image is not found
                        layers.Layer().Padding(30).Border(4).BorderColor("#1e3a8a");
                        layers.Layer().Padding(36).Border(1).BorderColor("#eab308");
                        layers.Layer().Background(Colors.White);
                    }

                    // Foreground Content Layer
                    // We use padding to ensure text stays inside the decorative border
                    layers.PrimaryLayer().Padding(90).Column(col =>
                    {
                        col.Spacing(20);

                        // Header
                        col.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                            .FontFamily("Times New Roman")
                            .FontSize(42)
                            .Bold()
                            .FontColor("#0f172a");

                        col.Item().PaddingTop(15).AlignCenter().Text("THIS IS PROUDLY PRESENTED TO")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken2)
                            .SemiBold();

                        // Student Name
                        col.Item().PaddingTop(10).AlignCenter().Text(_studentName)
                            .FontFamily("Times New Roman")
                            .FontSize(46)
                            .Italic()
                            .FontColor(Colors.Black);

                        // Divider Line
                        col.Item().AlignCenter().Container().Width(400).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        // Course Text
                        col.Item().PaddingTop(10).AlignCenter().Text("For successfully completing the course:")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken2);

                        col.Item().AlignCenter().Text($"\"{_courseTitle}\"")
                            .FontSize(24)
                            .Bold()
                            .FontColor("#0f172a");

                        col.Item().PaddingTop(5).AlignCenter().Text("Demonstrating outstanding dedication and mastery of the subject matter.")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);

                        // Spacer to push signatures down
                        col.Item().ExtendVertical();

                        // Signatures & Footer
                        col.Item().PaddingBottom(10).Row(footerRow =>
                        {
                            // Left: Date
                            footerRow.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text(_completionDate.ToString("MMMM dd, yyyy"))
                                    .FontSize(14)
                                    .FontColor(Colors.Black)
                                    .Bold();
                                
                                c.Item().PaddingTop(5).Container().Width(180).LineHorizontal(1).LineColor(Colors.Black);
                                
                                c.Item().PaddingTop(2).AlignCenter().Text("DATE")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken2)
                                    .Bold();
                            });

                            // Center: Gold Badge Placeholder
                            footerRow.ConstantItem(100).AlignCenter().AlignMiddle().Column(badge => 
                            {
                                badge.Item().Width(70).Height(70).Background("#eab308").AlignCenter().AlignMiddle().Column(c => 
                                {
                                    c.Item().AlignCenter().Text("★★★").FontSize(10).FontColor(Colors.White);
                                    c.Item().AlignCenter().Text(DateTime.Now.Year.ToString()).FontSize(14).Bold().FontColor(Colors.White);
                                    c.Item().AlignCenter().Text("AWARDED").FontSize(8).FontColor(Colors.White);
                                });
                            });

                            // Right: Signature
                            footerRow.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Jane Kane")
                                    .FontFamily("Times New Roman")
                                    .FontSize(26)
                                    .Italic()
                                    .FontColor(Colors.Black);
                                
                                c.Item().Container().Width(180).LineHorizontal(1).LineColor(Colors.Black);
                                
                                c.Item().PaddingTop(2).AlignCenter().Text("TRAINING DIRECTOR")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken2)
                                    .Bold();
                            });
                        });
                        
                        // Verification ID at the very bottom
                        col.Item().AlignCenter().Text($"Verification ID: {_certificateNumber}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }
    }
}