using AITrainingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Domain.Entities
{
    public class MediaFile
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string StoredFileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public Guid LessonId { get; set; }

        public Lesson Lesson { get; set; } = null!;

        public MediaType MediaType { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
    }
}
