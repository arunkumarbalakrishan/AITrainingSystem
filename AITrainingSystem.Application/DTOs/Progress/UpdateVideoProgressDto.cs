using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Progress
{
    public class UpdateVideoProgressDto
    {
        public Guid LessonId { get; set; }

        public int LastWatchedSecond { get; set; }

        public int TotalDurationSeconds { get; set; }
    }
}
