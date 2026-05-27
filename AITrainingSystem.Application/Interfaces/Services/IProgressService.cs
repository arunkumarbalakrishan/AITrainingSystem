using AITrainingSystem.Application.DTOs.Progress;
using AITrainingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public  interface IProgressService
    {
        Task UpdateVideoProgressAsync( Guid userId, UpdateVideoProgressDto dto);

        Task<VideoProgress?> GetVideoProgressAsync( Guid userId,Guid lessonId);
        Task<List<ContinueWatchingDto>>GetContinueWatchingAsync(Guid userId);
    }
}
