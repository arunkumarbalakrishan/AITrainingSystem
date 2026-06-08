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
    public class QuizRepository : IQuizRepository
    {
        private readonly ApplicationDbContext _context;

        public QuizRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Quiz> CreateAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            return quiz;
        }

        public async Task<Quiz?> GetByIdAsync(Guid id)
        {
            return await _context.Quizzes.FindAsync(id);
        }

        public async Task<Quiz?> GetByIdWithQuestionsAsync(Guid id)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IEnumerable<Quiz>> GetByCourseIdAsync(Guid courseId)
        {
            return await _context.Quizzes
                .Where(q => q.CourseId == courseId)
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .ToListAsync();
        }

        public async Task<Quiz?> GetFinalQuizByCourseIdAsync(Guid courseId)
        {
            return await _context.Quizzes
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.IsFinal);
        }

        public async Task UpdateAsync(Quiz quiz)
        {
            _context.Quizzes.Update(quiz);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Quiz quiz)
        {
            _context.Quizzes.Remove(quiz);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
