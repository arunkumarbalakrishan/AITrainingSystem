using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Features.Media.DTOs;

public class UploadMediaRequestDto
{
    public IFormFile File { get; set; } = null!;

    public Guid LessonId { get; set; }
}
