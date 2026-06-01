using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Domain.Entities
{
    public class Certificate
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid CourseId { get; set; }

        public string CertificateNumber { get; set; } = string.Empty;

        public DateTime IssuedDate { get; set; }

        public bool IsValid { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation Properties

        public User User { get; set; } = null!;

        public Course Course { get; set; } = null!;
    }
}
