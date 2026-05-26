using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Features.Media.DTOs
{
    public class SaveVideoProgressDto
    {
        public Guid LessonId { get; set; }

        public int LastWatchedSecond { get; set; }

        public decimal WatchPercentage { get; set; }
    }
}
