using System;

namespace AITrainingSystem.Domain.Entities
{
    public class LiveClassRecording
    {
        public Guid Id { get; set; }
        
        public Guid LiveClassId { get; set; }
        public LiveClass LiveClass { get; set; } = null!;
        
        public string FilePath { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        
        public long FileSizeInBytes { get; set; }
        public double DurationInMinutes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
