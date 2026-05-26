using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Domain.Entities;

public class VideoProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid LessonId { get; set; }

    public int LastWatchedSecond { get; set; }

    public decimal WatchPercentage { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime UpdatedAt { get; set; }
}
