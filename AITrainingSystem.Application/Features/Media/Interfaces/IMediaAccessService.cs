using AITrainingSystem.Application.Features.Media.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Features.Media.Interfaces
{
    public interface IMediaAccessService
    {
        Task<MediaAccessResponseDto>GetLessonVideoAsync( Guid lessonId, Guid userId);

        Task<MediaAccessResponseDto> GetLessonPdfAsync( Guid lessonId, Guid userId);
    }
}
