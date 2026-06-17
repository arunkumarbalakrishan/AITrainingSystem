using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Quiz;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AITrainingSystem.Infrastructure.Services.Assessment
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IQuizRepository _quizRepo;
        private readonly IAssessmentResultRepository _resultRepo;
        private readonly ILessonProgressRepository _lessonProgressRepo;
        private readonly ICertificateService _certificateService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<AssessmentService> _logger;

        public AssessmentService(
            IQuizRepository quizRepo,
            IAssessmentResultRepository resultRepo,
            ILessonProgressRepository lessonProgressRepo,
            ICertificateService certificateService,
            INotificationService notificationService,
            IUserRepository userRepo,
            ILogger<AssessmentService> logger)
        {
            _quizRepo = quizRepo;
            _resultRepo = resultRepo;
            _lessonProgressRepo = lessonProgressRepo;
            _certificateService = certificateService;
            _notificationService = notificationService;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<QuizDto>> CreateQuizAsync(CreateQuizDto dto)
        {
            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                CourseId = dto.CourseId,
                Title = dto.Title,
                Description = dto.Description,
                PassingScore = dto.PassingScore,
                IsFinal = dto.IsFinal,
                CreatedAt = DateTime.UtcNow,
                Questions = dto.Questions.Select(q => new Question
                {
                    Id = Guid.NewGuid(),
                    Text = q.Text,
                    Points = q.Points,
                    QuestionType = q.QuestionType,
                    Options = q.Options.Select(o => new QuestionOption
                    {
                        Id = Guid.NewGuid(),
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()
            };

            await _quizRepo.CreateAsync(quiz);
            await _quizRepo.SaveChangesAsync();

            return await GetQuizByIdAsync(quiz.Id);
        }

        public async Task<ApiResponse<QuizDto>> GetQuizByIdAsync(Guid quizId)
        {
            var quiz = await _quizRepo.GetByIdWithQuestionsAsync(quizId);
            if (quiz == null)
            {
                return ApiResponse<QuizDto>.FailResponse("Quiz not found.");
            }

            var dto = new QuizDto
            {
                Id = quiz.Id,
                CourseId = quiz.CourseId,
                Title = quiz.Title,
                Description = quiz.Description,
                PassingScore = quiz.PassingScore,
                IsFinal = quiz.IsFinal,
                Questions = quiz.Questions.Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Points = q.Points,
                    QuestionType = q.QuestionType,
                    Options = q.Options.Select(o => new OptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        IsCorrect = null // Hide correct status for security during quiz attempts
                    }).ToList()
                }).ToList()
            };

            return ApiResponse<QuizDto>.SuccessResponse(dto, "Quiz retrieved successfully.");
        }

        public async Task<ApiResponse<IEnumerable<QuizDto>>> GetQuizzesByCourseIdAsync(Guid courseId)
        {
            var quizzes = await _quizRepo.GetByCourseIdAsync(courseId);
            var result = quizzes.Select(q => new QuizDto
            {
                Id = q.Id,
                CourseId = q.CourseId,
                Title = q.Title,
                Description = q.Description,
                PassingScore = q.PassingScore,
                IsFinal = q.IsFinal,
                Questions = q.Questions.Select(ques => new QuestionDto
                {
                    Id = ques.Id,
                    Text = ques.Text,
                    Points = ques.Points,
                    QuestionType = ques.QuestionType,
                    Options = ques.Options.Select(o => new OptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        IsCorrect = null
                    }).ToList()
                }).ToList()
            });

            return ApiResponse<IEnumerable<QuizDto>>.SuccessResponse(result, "Quizzes retrieved successfully.");
        }

        public async Task<ApiResponse<AssessmentResultDto>> SubmitQuizAsync(Guid userId, QuizSubmitDto submission)
        {
            var quiz = await _quizRepo.GetByIdWithQuestionsAsync(submission.QuizId);
            if (quiz == null)
            {
                return ApiResponse<AssessmentResultDto>.FailResponse("Quiz not found.");
            }

            int totalPoints = quiz.Questions.Sum(q => q.Points);
            int earnedPoints = 0;

            var userAnswers = new List<UserAnswer>();

            foreach (var answerDto in submission.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
                if (question != null)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId);
                    if (selectedOption != null)
                    {
                        if (selectedOption.IsCorrect)
                        {
                            earnedPoints += question.Points;
                        }

                        userAnswers.Add(new UserAnswer
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = answerDto.QuestionId,
                            SelectedOptionId = answerDto.SelectedOptionId
                        });
                    }
                }
            }

            int percentageScore = totalPoints > 0 ? (int)Math.Round((double)earnedPoints / totalPoints * 100) : 0;
            bool isPassed = percentageScore >= quiz.PassingScore;

            var result = new AssessmentResult
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuizId = submission.QuizId,
                Score = percentageScore,
                IsPassed = isPassed,
                SubmittedAt = DateTime.UtcNow,
                Answers = userAnswers
            };

            await _resultRepo.CreateAsync(result);
            await _resultRepo.SaveChangesAsync();

            // IN-FLOW ASSESSMENT & CERTIFICATE TRIGGER
            if (quiz.IsFinal && isPassed)
            {
                _logger.LogInformation("User {UserId} passed final assessment for Course {CourseId} with {Score}%", userId, quiz.CourseId, percentageScore);
                
                // Check if student completed all lessons
                var completedCount = await _lessonProgressRepo.GetCompletedCountAsync(userId, quiz.CourseId);
                var totalLessons = await _lessonProgressRepo.GetTotalLessonsAsync(quiz.CourseId);

                if (totalLessons > 0 && completedCount == totalLessons)
                {
                    _logger.LogInformation("Generating certificate. User {UserId} completed all lessons ({Completed}/{Total}) and passed final assessment.", userId, completedCount, totalLessons);
                    await _certificateService.GenerateCertificateAsync(userId, quiz.CourseId);
                }
            }

            var responseDto = new AssessmentResultDto
            {
                Id = result.Id,
                UserId = result.UserId,
                QuizId = result.QuizId,
                Score = result.Score,
                IsPassed = result.IsPassed,
                SubmittedAt = result.SubmittedAt
            };

            return ApiResponse<AssessmentResultDto>.SuccessResponse(responseDto, "Quiz graded and submitted successfully.");
        }

        public async Task<ApiResponse<IEnumerable<AssessmentResultDto>>> GetUserResultsAsync(Guid userId)
        {
            var results = await _resultRepo.GetByUserIdAsync(userId);
            var response = results.Select(r => new AssessmentResultDto
            {
                Id = r.Id,
                UserId = r.UserId,
                QuizId = r.QuizId,
                Score = r.Score,
                IsPassed = r.IsPassed,
                SubmittedAt = r.SubmittedAt
            });

            return ApiResponse<IEnumerable<AssessmentResultDto>>.SuccessResponse(response, "Assessment results retrieved successfully.");
        }

        public async Task<bool> HasUserPassedFinalAssessmentAsync(Guid userId, Guid courseId)
        {
            var finalQuiz = await _quizRepo.GetFinalQuizByCourseIdAsync(courseId);
            if (finalQuiz == null)
            {
                // If there's no final quiz configured for the course, count it as passed/not required
                return true;
            }

            var bestResult = await _resultRepo.GetBestResultAsync(userId, finalQuiz.Id);
            return bestResult != null && bestResult.IsPassed;
        }
    }
}
