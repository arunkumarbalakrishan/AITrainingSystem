using AITrainingSystem.Application.DTOs.Lesson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Course
{
    public class CourseWithLessonsDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<LessonResponseDto> Lessons { get; set; } = new();
    }
}
