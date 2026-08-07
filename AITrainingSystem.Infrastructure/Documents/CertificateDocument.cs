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
                    // Background Layer (Maroon & Gold themed border image)
                    if (bgBytes != null)
                    {
                        layers.Layer().Image(bgBytes).FitArea();
                    }
                    else
                    {
                        // Fallback classic maroon/gold border if the image is not found
                        layers.Layer().Padding(30).Border(4).BorderColor("#7a1c1c");
                        layers.Layer().Padding(36).Border(1).BorderColor("#c5a059");
                        layers.Layer().Background(Colors.White);
                    }

                    // Foreground Content Layer with elegant padding to align text perfectly
                    layers.PrimaryLayer().PaddingTop(50).PaddingBottom(35).PaddingLeft(90).PaddingRight(90).ShowEntire().Column(col =>
                    {
                        col.Spacing(2);

                        // Header Block
                        col.Item().AlignCenter().Text("CERTIFICATE")
                            .FontFamily("Arial")
                            .FontSize(36)
                            .Bold()
                            .FontColor("#0f172a");

                        col.Item().AlignCenter().Container().Width(240).PaddingBottom(1).LineHorizontal(1.5f).LineColor("#c5a059");

                        col.Item().AlignCenter().Text("O F   A C H I E V E M E N T")
                            .FontFamily("Arial")
                            .FontSize(10.5f)
                            .Bold()
                            .FontColor("#0f172a");

                        col.Item().PaddingTop(15).AlignCenter().Text("THIS CERTIFICATE IS PROUDLY PRESENTED TO")
                            .FontFamily("Arial")
                            .FontSize(9.0f)
                            .Bold()
                            .FontColor("#475569");

                        // Recipient Name (Maroon, Times New Roman, bold italic)
                        col.Item().PaddingTop(3).AlignCenter().Text(_studentName)
                            .FontFamily("Times New Roman")
                            .FontSize(40)
                            .Bold()
                            .Italic()
                            .FontColor("#7a1c1c");

                        col.Item().AlignCenter().Container().Width(300).PaddingTop(2).PaddingBottom(10).LineHorizontal(1).LineColor("#c5a059");

                        // Course details description block
                        col.Item().AlignCenter().Text("for successfully completing the specialized course path")
                            .FontFamily("Arial")
                            .FontSize(10.5f)
                            .FontColor("#475569");

                        col.Item().AlignCenter().Text($"\"{_courseTitle}\"")
                            .FontFamily("Arial")
                            .FontSize(19)
                            .Bold()
                            .FontColor("#0f172a");

                        col.Item().PaddingTop(2).AlignCenter().Text("demonstrating exceptional analytical capabilities, professional commitment,")
                            .FontFamily("Arial")
                            .FontSize(9.0f)
                            .FontColor("#475569");
                        col.Item().AlignCenter().Text("and comprehensive mastery of the curriculum context.")
                            .FontFamily("Arial")
                            .FontSize(9.0f)
                            .FontColor("#475569");

                        // Spacer to push footer elements down
                        col.Item().Height(25);

                        // Signatures & Badges row at the bottom
                        col.Item().PaddingBottom(5).Row(footerRow =>
                        {
                            // Left: Date
                            footerRow.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text(_completionDate.ToString("MMMM dd, yyyy"))
                                    .FontFamily("Arial")
                                    .FontSize(11)
                                    .FontColor("#0f172a")
                                    .Bold();
                                
                                c.Item().PaddingTop(3).Container().Width(130).LineHorizontal(1).LineColor("#475569");
                                
                                c.Item().PaddingTop(3).AlignCenter().Text("DATE OF ISSUANCE")
                                    .FontFamily("Arial")
                                    .FontSize(7.0f)
                                    .FontColor("#475569")
                                    .Bold();
                            });

                            // Center: Elegant Round Maroon & Gold Sealed Badge
                            footerRow.ConstantItem(90).AlignCenter().AlignMiddle().Column(badgeColumn => 
                            {
                                badgeColumn.Item().Width(58).Height(58).CornerRadius(29).Background("#7a1c1c").Border(2.2f, "#c5a059").AlignCenter().AlignMiddle().Column(c => 
                                {
                                    c.Item().AlignCenter().Text("★ ★ ★").FontSize(7.0f).FontColor("#c5a059").Bold();
                                    c.Item().AlignCenter().Text("AI").FontSize(13).Bold().FontColor(Colors.White);
                                    c.Item().AlignCenter().Text("LMS CERTIFIED").FontSize(5.5f).FontColor("#c5a059").Bold();
                                });
                            });

                            // Right: Training Director Signature
                            footerRow.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Jane Kane")
                                    .FontFamily("Times New Roman")
                                    .FontSize(20)
                                    .Italic()
                                    .Bold()
                                    .FontColor("#7a1c1c");
                                
                                c.Item().PaddingTop(3).Container().Width(130).LineHorizontal(1).LineColor("#475569");
                                
                                c.Item().PaddingTop(3).AlignCenter().Text("TRAINING DIRECTOR")
                                    .FontFamily("Arial")
                                    .FontSize(7.0f)
                                    .FontColor("#475569")
                                    .Bold();
                            });
                        });
                        
                        // Verification Identification row at the very bottom
                        col.Item().AlignCenter().Text($"Verification ID: {_certificateNumber}   |   Verify online at: {_verificationUrl}")
                            .FontFamily("Arial")
                            .FontSize(7.0f)
                            .FontColor("#94a3b8");
                    });
                });
            });
        }
    }
}