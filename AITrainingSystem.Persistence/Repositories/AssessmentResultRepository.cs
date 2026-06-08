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
    public class AssessmentResultRepository : IAssessmentResultRepository
    {
        private readonly ApplicationDbContext _context;

        public AssessmentResultRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssessmentResult> CreateAsync(AssessmentResult result)
        {
            await _context.AssessmentResults.AddAsync(result);
            return result;
        }

        public async Task<AssessmentResult?> GetByIdAsync(Guid id)
        {
            return await _context.AssessmentResults
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<AssessmentResult>> GetByUserIdAsync(Guid userId)
        {
            return await _context.AssessmentResults
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<AssessmentResult?> GetBestResultAsync(Guid userId, Guid quizId)
        {
            return await _context.AssessmentResults
                .Where(r => r.UserId == userId && r.QuizId == quizId)
                .OrderByDescending(r => r.Score)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
