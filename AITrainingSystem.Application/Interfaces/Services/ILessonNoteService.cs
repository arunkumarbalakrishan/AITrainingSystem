using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.LessonNote;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface ILessonNoteService
    {
        Task<ApiResponse<LessonNoteDto>> CreateNoteAsync(Guid userId, CreateLessonNoteDto dto);
        Task<ApiResponse<IEnumerable<LessonNoteDto>>> GetNotesByLessonAsync(Guid userId, Guid lessonId);
        Task<ApiResponse<LessonNoteDto>> UpdateNoteAsync(Guid userId, Guid noteId, UpdateLessonNoteDto dto);
        Task<ApiResponse<bool>> DeleteNoteAsync(Guid userId, Guid noteId);
    }
}
