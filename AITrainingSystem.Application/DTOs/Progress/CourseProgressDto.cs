using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Progress
{
    public class CourseProgressDto
    {
        public Guid CourseId { get; set; }

        public int CompletedLessons { get; set; }

        public int TotalLessons { get; set; }

        public double ProgressPercentage { get; set; }

        public bool IsCourseCompleted { get; set; }

        public bool IsCertificateEligible { get; set; }

        public bool HasStarted { get; set; }
    }
}
