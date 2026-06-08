using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.LessonNote;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;

namespace AITrainingSystem.Infrastructure.Services.Notes
{
    public class LessonNoteService : ILessonNoteService
    {
        private readonly ILessonNoteRepository _repo;

        public LessonNoteService(ILessonNoteRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiResponse<LessonNoteDto>> CreateNoteAsync(Guid userId, CreateLessonNoteDto dto)
        {
            var note = new LessonNote
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LessonId = dto.LessonId,
                Content = dto.Content,
                VideoTimestampSeconds = dto.VideoTimestampSeconds,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(note);
            await _repo.SaveChangesAsync();

            var result = MapToDto(note);
            return ApiResponse<LessonNoteDto>.SuccessResponse(result, "Lesson note created successfully.");
        }

        public async Task<ApiResponse<IEnumerable<LessonNoteDto>>> GetNotesByLessonAsync(Guid userId, Guid lessonId)
        {
            var notes = await _repo.GetByLessonIdAndUserIdAsync(lessonId, userId);
            var result = notes.Select(MapToDto);
            return ApiResponse<IEnumerable<LessonNoteDto>>.SuccessResponse(result, "Lesson notes retrieved successfully.");
        }

        public async Task<ApiResponse<LessonNoteDto>> UpdateNoteAsync(Guid userId, Guid noteId, UpdateLessonNoteDto dto)
        {
            var note = await _repo.GetByIdAsync(noteId);
            if (note == null)
            {
                return ApiResponse<LessonNoteDto>.FailResponse("Lesson note not found.");
            }

            if (note.UserId != userId)
            {
                return ApiResponse<LessonNoteDto>.FailResponse("Unauthorized to update this note.");
            }

            note.Content = dto.Content;
            note.VideoTimestampSeconds = dto.VideoTimestampSeconds;
            note.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(note);
            await _repo.SaveChangesAsync();

            var result = MapToDto(note);
            return ApiResponse<LessonNoteDto>.SuccessResponse(result, "Lesson note updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteNoteAsync(Guid userId, Guid noteId)
        {
            var note = await _repo.GetByIdAsync(noteId);
            if (note == null)
            {
                return ApiResponse<bool>.FailResponse("Lesson note not found.");
            }

            if (note.UserId != userId)
            {
                return ApiResponse<bool>.FailResponse("Unauthorized to delete this note.");
            }

            await _repo.DeleteAsync(note);
            await _repo.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Lesson note deleted successfully.");
        }

        private static LessonNoteDto MapToDto(LessonNote note)
        {
            return new LessonNoteDto
            {
                Id = note.Id,
                UserId = note.UserId,
                LessonId = note.LessonId,
                Content = note.Content,
                VideoTimestampSeconds = note.VideoTimestampSeconds,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }
    }
}
