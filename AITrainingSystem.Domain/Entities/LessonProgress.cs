using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Domain.Entities
{
    public class LessonProgress
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid LessonId { get; set; }
        public Lesson? Lesson { get; set; } 

        public bool IsCompleted { get; set; }

        public DateTime CompletedAt { get; set; }
    }
}
