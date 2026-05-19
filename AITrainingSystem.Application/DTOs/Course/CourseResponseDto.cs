using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.DTOs.Course;

public class CourseResponseDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationInHours { get; set; }

    public string? ThumbnailUrl { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }
}
