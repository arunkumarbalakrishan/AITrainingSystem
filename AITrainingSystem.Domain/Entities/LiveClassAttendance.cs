using System;

namespace AITrainingSystem.Domain.Entities
{
    public class LiveClassAttendance
    {
        public Guid Id { get; set; }
        
        public Guid LiveClassId { get; set; }
        public LiveClass LiveClass { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string UserName { get; set; } = string.Empty;
        
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        
        public double? DurationInMinutes { get; set; }
    }
}
