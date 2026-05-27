using AITrainingSystem.Application.DTOs.Progress;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Infrastructure.Services.Progress
{
    public class ProgressService : IProgressService
    {
        private readonly ApplicationDbContext _context;

        public ProgressService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task UpdateVideoProgressAsync( Guid userId, UpdateVideoProgressDto dto)
        {
            // Find existing progress
            var progress = await _context.VideoProgresses
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.LessonId == dto.LessonId);

            // If no progress exists, create new
            if (progress == null)
            {
                progress = new VideoProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LessonId = dto.LessonId
                };

                _context.VideoProgresses.Add(progress);
            }

            // Validate duration
            if (dto.TotalDurationSeconds <= 0)
            {
                throw new ArgumentException("Video duration must be greater than zero.");
            }

            // Prevent watched seconds exceeding duration
            var watchedSeconds = Math.Min(
                    dto.LastWatchedSecond,
                    dto.TotalDurationSeconds);

            // Calculate watch percentage
            var percentage =
                ((decimal)dto.LastWatchedSecond /
                 dto.TotalDurationSeconds) * 100;

            // Completion rule
            var isCompleted = percentage >= 90;

            // Update values
            progress.LastWatchedSecond =
                        watchedSeconds;

            progress.TotalDurationSeconds =
                dto.TotalDurationSeconds;

            progress.WatchPercentage =
                Math.Round(percentage, 2);

            progress.IsCompleted =
                isCompleted;

            progress.UpdatedAt =
                DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();
        }

        public async Task<VideoProgress?> GetVideoProgressAsync( Guid userId,Guid lessonId)
        {
            return await _context.VideoProgresses
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.LessonId == lessonId);
        }
        public async Task<List<ContinueWatchingDto>> GetContinueWatchingAsync(Guid userId)
        {
            return await _context.VideoProgresses
                .Include(x => x.Lesson)
                .ThenInclude(x => x.Course)
                .Where(x =>
                    x.UserId == userId &&
                    x.LastWatchedSecond > 0 &&
                    !x.IsCompleted)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new ContinueWatchingDto
                {
                    CourseId = x.Lesson.CourseId,
                    CourseTitle = x.Lesson.Course.Title,

                    LessonId = x.LessonId,
                    LessonTitle = x.Lesson.Title,

                    LastWatchedSecond =
                        x.LastWatchedSecond,

                    WatchPercentage =
                        x.WatchPercentage,

                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();
        }

    }
}
