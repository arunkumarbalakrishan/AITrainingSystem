using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Analytics;

public class LearningAnalyticsDto
{
    public int TotalCoursesEnrolled { get; set; }

    public int CompletedCourses { get; set; }

    public int InProgressCourses { get; set; }

    public int CertificatesEarned { get; set; }

    public int TotalHours { get; set; }

    public int StreakDays { get; set; }
}
