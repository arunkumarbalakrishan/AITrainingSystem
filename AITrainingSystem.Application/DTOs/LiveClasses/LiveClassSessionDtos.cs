using System;

namespace AITrainingSystem.Application.DTOs.LiveClasses
{
    public class LiveClassAttendanceDto
    {
        public Guid Id { get; set; }
        public Guid LiveClassId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public double? DurationInMinutes { get; set; }
    }

    public class LiveClassRecordingDto
    {
        public Guid Id { get; set; }
        public Guid LiveClassId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public double DurationInMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddRecordingDto
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public double DurationInMinutes { get; set; }
    }
}
