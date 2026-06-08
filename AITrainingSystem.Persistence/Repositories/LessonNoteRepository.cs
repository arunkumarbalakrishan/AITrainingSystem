using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AITrainingSystem.Persistence.Repositories
{
    public class LessonNoteRepository : ILessonNoteRepository
    {
        private readonly ApplicationDbContext _context;

        public LessonNoteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LessonNote> CreateAsync(LessonNote note)
        {
            await _context.LessonNotes.AddAsync(note);
            return note;
        }

        public async Task<LessonNote?> GetByIdAsync(Guid id)
        {
            return await _context.LessonNotes.FindAsync(id);
        }

        public async Task<IEnumerable<LessonNote>> GetByLessonIdAndUserIdAsync(Guid lessonId, Guid userId)
        {
            return await _context.LessonNotes
                .Where(n => n.LessonId == lessonId && n.UserId == userId)
                .OrderBy(n => n.VideoTimestampSeconds)
                .ThenBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(LessonNote note)
        {
            _context.LessonNotes.Update(note);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(LessonNote note)
        {
            _context.LessonNotes.Remove(note);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
