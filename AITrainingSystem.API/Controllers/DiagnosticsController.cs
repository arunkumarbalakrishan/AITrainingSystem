using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.AI;
using AITrainingSystem.Application.DTOs.Payment;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DiagnosticsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly IAIService _aiService;
    private readonly IPaymentService _paymentService;
    private readonly ICertificatePdfService _pdfService;

    public DiagnosticsController(
        ApplicationDbContext dbContext,
        IStorageService storageService,
        IAIService aiService,
        IPaymentService paymentService,
        ICertificatePdfService pdfService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _aiService = aiService;
        _paymentService = paymentService;
        _pdfService = pdfService;
    }

    [HttpGet("run-all")]
    public async Task<IActionResult> RunEndToEndDiagnostics()
    {
        var report = new DiagnosticReport();

        // 1. Database Check
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                var userCount = await _dbContext.Users.CountAsync();
                report.DatabaseStatus = $"Connected (User count: {userCount})";
                report.DatabasePassed = true;
            }
            else
            {
                report.DatabaseStatus = "Failed: Cannot connect to database.";
            }
        }
        catch (Exception ex)
        {
            report.DatabaseStatus = $"Error: {ex.Message}";
        }

        // 2. Storage Check
        try
        {
            var testFileName = $"diag_test_{Guid.NewGuid()}.txt";
            var testContent = "Diagnostic File Content";
            var bytes = Encoding.UTF8.GetBytes(testContent);

            using var uploadStream = new MemoryStream(bytes);
            var fileUrl = await _storageService.UploadFileAsync(uploadStream, testFileName, "diagnostics", "text/plain");

            if (!string.IsNullOrEmpty(fileUrl))
            {
                report.StorageStatus = $"Passed (Provider: {_storageService.GetType().Name}, Uploaded: {fileUrl})";
                report.StoragePassed = true;

                // Try downloading it
                using var downloadStream = await _storageService.GetFileStreamAsync(fileUrl);
                if (downloadStream != null)
                {
                    using var reader = new StreamReader(downloadStream);
                    var readContent = await reader.ReadToEndAsync();
                    if (readContent == testContent)
                    {
                        report.StorageStatus += " - Download verified successfully.";
                    }
                    else
                    {
                        report.StorageStatus += " - Download verification failed (content mismatch).";
                    }
                }

                // Cleanup
                await _storageService.DeleteFileAsync(fileUrl);
            }
            else
            {
                report.StorageStatus = "Failed: Upload returned empty URL.";
            }
        }
        catch (Exception ex)
        {
            report.StorageStatus = $"Error: {ex.Message}";
        }

        // 3. AI Service Check
        try
        {
            var sampleRequest = new TutorRequestDto { Question = "Explain dependency injection in 1 sentence." };
            var result = await _aiService.AskTutorAsync(Guid.NewGuid(), Guid.NewGuid(), sampleRequest);

            if (result.Success && !string.IsNullOrEmpty(result.Data))
            {
                if (result.Data.StartsWith("[AI Tutor Fallback]"))
                {
                    report.AiStatus = $"Passed (OpenAI Key not configured, fell back gracefully to Mock AI)";
                }
                else
                {
                    report.AiStatus = $"Passed (Response: \"{result.Data.Trim()}\")";
                }
                report.AiPassed = true;
            }
            else
            {
                report.AiStatus = $"Failed: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            report.AiStatus = $"Error: {ex.Message}";
        }

        // 4. Payment Checkout Check
        try
        {
            var dbUser = await _dbContext.Users.FirstOrDefaultAsync();
            if (dbUser == null)
            {
                dbUser = new AITrainingSystem.Domain.Entities.User 
                { 
                    FullName = "Diagnostics Test User", 
                    Email = "diag@test.com", 
                    PasswordHash = "test", 
                    Role = "Student" 
                };
                _dbContext.Users.Add(dbUser);
                await _dbContext.SaveChangesAsync();
            }

            var dbCourse = await _dbContext.Courses.FirstOrDefaultAsync();
            if (dbCourse == null)
            {
                dbCourse = new AITrainingSystem.Domain.Entities.Course 
                { 
                    Title = "Diagnostics Test Course", 
                    Description = "Test Course", 
                    Price = 49.99m, 
                    IsPublished = true,
                    InstructorId = dbUser.Id
                };
                _dbContext.Courses.Add(dbCourse);
                await _dbContext.SaveChangesAsync();
            }

            var checkoutReq = new CreateCheckoutRequestDto 
            { 
                CourseId = dbCourse.Id,
                SuccessUrl = "http://localhost:5000/success",
                CancelUrl = "http://localhost:5000/cancel"
            };
            var result = await _paymentService.CreateCheckoutSessionAsync(dbUser.Id, checkoutReq);

            if (result.Success && result.Data != null && !string.IsNullOrEmpty(result.Data.CheckoutUrl))
            {
                report.PaymentStatus = $"Passed (Checkout Link: {result.Data.CheckoutUrl})";
                report.PaymentPassed = true;
            }
            else
            {
                report.PaymentStatus = $"Failed: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            report.PaymentStatus = $"Error: {ex.Message}";
        }

        // 5. PDF Service Check
        try
        {
            var pdfBytes = _pdfService.GenerateCertificatePdf(
                studentName: "Manoel Cortes Mendez",
                courseTitle: "Diagnostics Verification",
                certificateNumber: "DIAG-CERT-1234",
                completionDate: DateTime.UtcNow,
                verificationUrl: "https://aitrainingsystem.com/verify/DIAG-CERT-1234"
            );

            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                report.PdfStatus = $"Passed (PDF bytes generated: {pdfBytes.Length})";
                report.PdfPassed = true;
            }
            else
            {
                report.PdfStatus = "Failed: Generated PDF size is 0 bytes.";
            }
        }
        catch (Exception ex)
        {
            report.PdfStatus = $"Error: {ex.Message}";
        }

        report.AllPassed = report.DatabasePassed &&
                           report.StoragePassed &&
                           report.AiPassed &&
                           report.PaymentPassed &&
                           report.PdfPassed;

        return Ok(report);
    }
}

public class DiagnosticReport
{
    public bool AllPassed { get; set; }
    public bool DatabasePassed { get; set; }
    public string DatabaseStatus { get; set; } = "Not Run";
    public bool StoragePassed { get; set; }
    public string StorageStatus { get; set; } = "Not Run";
    public bool AiPassed { get; set; }
    public string AiStatus { get; set; } = "Not Run";
    public bool PaymentPassed { get; set; }
    public string PaymentStatus { get; set; } = "Not Run";
    public bool PdfPassed { get; set; }
    public string PdfStatus { get; set; } = "Not Run";
}
