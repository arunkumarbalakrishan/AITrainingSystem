using System;
using System.Collections.Generic;

namespace AITrainingSystem.Application.DTOs.Analytics
{
    public class AdminReportsDto
    {
        public decimal TotalRevenue { get; set; }
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
        public List<TransactionDto> RecentTransactions { get; set; } = new();
        public List<StudentProgressReportDto> StudentProgress { get; set; } = new();
        public List<TopCourseReportDto> TopCourses { get; set; } = new();
        public List<AdminCertificateDto> IssuedCertificates { get; set; } = new();
    }

    public class AdminCertificateDto
    {
        public Guid CertificateId { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TransactionDto
    {
        public Guid PaymentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "Pending";
        public DateTime Date { get; set; }
    }

    public class StudentProgressReportDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int CompletedLessonsCount { get; set; }
        public int TotalLessonsCount { get; set; }
        public double ProgressPercentage { get; set; }
        public bool HasCertificate { get; set; }
    }

    public class TopCourseReportDto
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
    }
}
