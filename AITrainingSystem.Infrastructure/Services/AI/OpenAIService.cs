using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.AI;
using AITrainingSystem.Application.DTOs.Quiz;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AITrainingSystem.Infrastructure.Services.AI
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIService> _logger;
        private readonly ICourseRepository _courseRepository;
        private readonly ILessonProgressRepository _lessonProgressRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IStorageService _storageService;

        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<OpenAIService> logger,
            ICourseRepository courseRepository,
            ILessonProgressRepository lessonProgressRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            ApplicationDbContext dbContext,
            IStorageService storageService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _courseRepository = courseRepository;
            _lessonProgressRepository = lessonProgressRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _dbContext = dbContext;
            _storageService = storageService;

            _apiKey = _configuration["OpenAI:ApiKey"] ?? string.Empty;
            _model = _configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
        }

        public async Task<ApiResponse<string>> AskTutorAsync(Guid userId, Guid courseId, TutorRequestDto request)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            var lesson = course?.Lessons?.FirstOrDefault(l => l.Id == request.LessonId);

            var lessonTitle = lesson?.Title ?? "this lesson";
            var courseTitle = course?.Title ?? "the course";

            var prompt = $"You are an expert AI Tutor for the online course '{courseTitle}'. " +
                         $"The student is currently learning the lesson '{lessonTitle}'.\n" +
                         $"Please answer the following student question with high-quality educational context and details:\n" +
                         $"Student Question: \"{request.Question}\"";

            try
            {
                var answer = await CallOpenAIApiAsync(prompt, "You are a helpful, precise computer science tutor.");
                return ApiResponse<string>.SuccessResponse(answer, "Tutor response generated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call OpenAI for tutor. Using conversational fallback response.");
                
                var q = request.Question.ToLowerInvariant();
                var fallbackAnswer = q switch
                {
                    _ when q.Contains("hello") || q.Contains("hi") || q.Contains("hey") => 
                        "Hello! I am your AI Assistant. How can I help you with your learning today?",
                    
                    _ when q.Contains("who are you") || q.Contains("what are you") => 
                        "I am the AITraining Assistant, an intelligent tutor designed to help you understand software concepts, debug code, and guide you through your courses.",
                    
                    _ when q.Contains("course") || q.Contains("lesson") => 
                        $"Regarding '{lessonTitle}' in '{courseTitle}': I'd be happy to explain this topic in more detail. Could you specify which part you are struggling with?",
                    
                    _ when q.Contains("code") || q.Contains("error") || q.Contains("bug") => 
                        "I can help with that! Please share the specific code snippet or error message you're looking at, and I'll walk you through the solution step-by-step.",
                    
                    _ when q.Contains("explain") || q.Contains("what is") || q.Contains("how") => 
                        $"That's a great question about {request.Question.Replace("what is", "").Replace("explain", "").Trim(' ', '?')}. This concept is foundational in modern software development. Let me break it down for you: First, it allows developers to write more maintainable systems. Second, it integrates seamlessly with existing patterns. Do you want me to show you a practical example?",
                    
                    _ => $"I understand you're asking about: \"{request.Question}\". As your AI Tutor, I'm here to help you master this material. Can you provide a bit more context so I can give you a precise and tailored answer?"
                };

                return ApiResponse<string>.SuccessResponse(fallbackAnswer, "Generated tutor fallback response.");
            }
        }

        public async Task<ApiResponse<QuizDto>> GenerateQuizAsync(QuizGenRequestDto request)
        {
            var prompt = $"Generate a multiple choice quiz about '{request.CourseTopic}' with exactly {request.QuestionCount} questions. " +
                         $"Return the quiz strictly formatted as a valid JSON object matching the schema below. " +
                         $"Do not include any markdown format tags (like ```json). Just return the raw JSON string.\n\n" +
                         $"Schema:\n" +
                         $"{{\n" +
                         $"  \"title\": \"Quiz Title\",\n" +
                         $"  \"description\": \"Quiz Description\",\n" +
                         $"  \"questions\": [\n" +
                         $"    {{\n" +
                         $"      \"text\": \"Question Text\",\n" +
                         $"      \"points\": 1,\n" +
                         $"      \"questionType\": \"SingleChoice\",\n" +
                         $"      \"options\": [\n" +
                         $"        {{ \"optionText\": \"Option text 1\", \"isCorrect\": true }},\n" +
                         $"        {{ \"optionText\": \"Option text 2\", \"isCorrect\": false }}\n" +
                         $"      ]\n" +
                         $"    }}\n" +
                         $"  ]\n" +
                         $"}}";

            try
            {
                var jsonResponse = await CallOpenAIApiAsync(prompt, "You are a quiz generation engine that returns raw JSON matching a specified schema.", true);
                
                // Parse generated JSON
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                var quizDto = new QuizDto
                {
                    Id = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    Title = root.GetProperty("title").GetString() ?? $"AI Generated Quiz for {request.CourseTopic}",
                    Description = root.GetProperty("description").GetString() ?? "Evaluate your learning.",
                    PassingScore = 70,
                    IsFinal = false,
                    Questions = new List<QuestionDto>()
                };

                var questionsElement = root.GetProperty("questions");
                foreach (var qElement in questionsElement.EnumerateArray())
                {
                    var questionDto = new QuestionDto
                    {
                        Id = Guid.NewGuid(),
                        Text = qElement.GetProperty("text").GetString() ?? string.Empty,
                        Points = qElement.TryGetProperty("points", out var p) ? p.GetInt32() : 1,
                        QuestionType = qElement.TryGetProperty("questionType", out var t) ? t.GetString() ?? "SingleChoice" : "SingleChoice",
                        Options = new List<OptionDto>()
                    };

                    var optionsElement = qElement.GetProperty("options");
                    foreach (var oElement in optionsElement.EnumerateArray())
                    {
                        questionDto.Options.Add(new OptionDto
                        {
                            Id = Guid.NewGuid(),
                            OptionText = oElement.GetProperty("optionText").GetString() ?? string.Empty,
                            IsCorrect = oElement.GetProperty("isCorrect").GetBoolean()
                        });
                    }

                    quizDto.Questions.Add(questionDto);
                }

                return ApiResponse<QuizDto>.SuccessResponse(quizDto, "AI Quiz generated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI quiz. Using mock fallback quiz.");
                return ApiResponse<QuizDto>.SuccessResponse(GetMockFallbackQuiz(request.CourseId, request.CourseTopic), "Generated fallback mock quiz.");
            }
        }

        public async Task<ApiResponse<IEnumerable<CourseRecommendationDto>>> GetRecommendationsAsync(Guid userId)
        {
            var allCourses = await _courseRepository.GetAllAsync();
            
            // Get completed courses of the student
            // For now, let's look at all courses and generate generic mock recommendations
            var prompt = $"Analyze the user learning profile. The user has registered interests in software training. " +
                         $"Here is the list of all available training courses:\n" +
                         string.Join("\n", allCourses.Select(c => $"- {c.Title} (ID: {c.Id})")) + "\n" +
                         $"Recommend exactly 2 courses. Return the response strictly as a JSON array of objects with schema:\n" +
                         $"[ {{\"courseId\": \"guid-value\", \"title\": \"Course Title\", \"reason\": \"Why recommend this?\"}} ]";

            try
            {
                var jsonResponse = await CallOpenAIApiAsync(prompt, "You are a course recommendation system returning raw JSON array.", true);
                using var doc = JsonDocument.Parse(jsonResponse);
                var recommendations = new List<CourseRecommendationDto>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var idStr = element.GetProperty("courseId").GetString();
                    Guid courseId = Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
                    recommendations.Add(new CourseRecommendationDto
                    {
                        CourseId = courseId,
                        Title = element.GetProperty("title").GetString() ?? string.Empty,
                        Reason = element.GetProperty("reason").GetString() ?? string.Empty
                    });
                }

                return ApiResponse<IEnumerable<CourseRecommendationDto>>.SuccessResponse(recommendations, "Recommendations generated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call OpenAI for recommendations. Using mock recommendations.");
                var mockRecs = allCourses.Take(2).Select(c => new CourseRecommendationDto
                {
                    CourseId = c.Id,
                    Title = c.Title,
                    Reason = "Based on your interest in software design and progress in training."
                }).ToList();

                return ApiResponse<IEnumerable<CourseRecommendationDto>>.SuccessResponse(mockRecs, "Fallback recommendations generated.");
            }
        }

        public async Task<ApiResponse<MockInterviewResponseDto>> ConductMockInterviewStepAsync(Guid userId, MockInterviewStepDto request)
        {
            var systemPrompt = $"You are conducting a professional mock interview for a software developer role on the topic of: {request.CourseTopic}. " +
                               $"Engage with the student in a conversational step-by-step manner. " +
                               $"If the student's answer is present, critique it constructively, and ask the next interview question. " +
                               $"Ask exactly 1 question at a time. " +
                               $"If the chat history has reached 4 exchanges (8 messages), say 'Thank you, this concludes the interview. Here is your evaluation:' and print a score out of 100 with comprehensive feedback.\n" +
                               $"Always output your response as a valid JSON object matching this schema:\n" +
                               $"{{\n" +
                               $"  \"nextQuestionOrFeedback\": \"Critique of answer (if any) and next question, or completion message\",\n" +
                               $"  \"score\": 85, // Nullable, only output integer score when interview finishes\n" +
                               $"  \"feedback\": \"Constructive summary feedback, or empty if ongoing\",\n" +
                               $"  \"isFinished\": false // true when completed\n" +
                               $"}}";

            var messages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "system", Content = systemPrompt }
            };

            messages.AddRange(request.ChatHistory);

            if (!string.IsNullOrEmpty(request.StudentAnswer))
            {
                messages.Add(new ChatMessageDto { Role = "user", Content = request.StudentAnswer });
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var payloadJson = JsonSerializer.Serialize(messages, options);
                var responseJson = await SendRawChatPayloadAsync(payloadJson, true);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var result = new MockInterviewResponseDto
                {
                    NextQuestionOrFeedback = root.GetProperty("nextQuestionOrFeedback").GetString() ?? string.Empty,
                    Score = root.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : null,
                    Feedback = root.TryGetProperty("feedback", out var f) ? f.GetString() ?? string.Empty : string.Empty,
                    IsFinished = root.TryGetProperty("isFinished", out var isFin) && isFin.GetBoolean()
                };

                return ApiResponse<MockInterviewResponseDto>.SuccessResponse(result, "Mock interview step completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call OpenAI for mock interview. Using fallback step.");
                var isFin = request.ChatHistory.Count >= 6;
                var fallback = new MockInterviewResponseDto
                {
                    NextQuestionOrFeedback = isFin 
                        ? "Interview completed successfully! Good job." 
                        : "Interesting response. Can you explain how you would handle asynchronous workflows in this context?",
                    Score = isFin ? 85 : null,
                    Feedback = isFin ? "Overall strong technical understanding, with room for improvement in scalability concepts." : string.Empty,
                    IsFinished = isFin
                };

                return ApiResponse<MockInterviewResponseDto>.SuccessResponse(fallback, "Mock interview step completed.");
            }
        }

        public async Task<ApiResponse<ResumeAnalysisResultDto>> AnalyzeResumeAsync(Guid userId, ResumeAnalysisRequestDto request)
        {
            var prompt = $"Analyze the following resume content:\n" +
                         $"\"\"\"{request.ResumeText}\"\"\"\n\n" +
                         $"Constructively critique the resume in context of technical training courses the student has completed. " +
                         $"Return the analysis strictly as a JSON object matching this schema:\n" +
                         $"{{\n" +
                         $"  \"matchScore\": 75, // integer percentage matching tech roles\n" +
                         $"  \"critique\": \"Review feedback...\",\n" +
                         $"  \"skillsStrengths\": [\"Strength 1\", \"Strength 2\"],\n" +
                         $"  \"suggestedCourses\": [\"Suggested Course 1\", \"Suggested Course 2\"]\n" +
                         $"}}";

            try
            {
                var jsonResponse = await CallOpenAIApiAsync(prompt, "You are a resume screening agent and career advisor returning raw JSON.", true);
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                var strengths = new List<string>();
                foreach (var el in root.GetProperty("skillsStrengths").EnumerateArray())
                {
                    strengths.Add(el.GetString() ?? string.Empty);
                }

                var suggested = new List<string>();
                foreach (var el in root.GetProperty("suggestedCourses").EnumerateArray())
                {
                    suggested.Add(el.GetString() ?? string.Empty);
                }

                var result = new ResumeAnalysisResultDto
                {
                    MatchScore = root.GetProperty("matchScore").GetInt32(),
                    Critique = root.GetProperty("critique").GetString() ?? string.Empty,
                    SkillsStrengths = strengths,
                    SuggestedCourses = suggested
                };

                await SendResumeAnalysisEmailAsync(userId, result);
                return ApiResponse<ResumeAnalysisResultDto>.SuccessResponse(result, "Resume analysis completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call OpenAI for resume analysis. Using fallback result.");
                var result = new ResumeAnalysisResultDto
                {
                    MatchScore = 70,
                    Critique = "Your resume shows good foundational knowledge. We recommend highlighting more practical course projects.",
                    SkillsStrengths = new List<string> { "Foundational Programming", "Database Concepts" },
                    SuggestedCourses = new List<string> { "Advanced ASP.NET Core API", "Enterprise Cloud Architecture" }
                };

                await SendResumeAnalysisEmailAsync(userId, result);
                return ApiResponse<ResumeAnalysisResultDto>.SuccessResponse(result, "Resume analysis completed.");
            }
        }

        private async Task SendResumeAnalysisEmailAsync(Guid userId, ResumeAnalysisResultDto result)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    var subject = "AI Resume Analysis Completed";
                    var body = $"<h3>Hello {user.FullName},</h3>" +
                               $"<p>Your resume has been analyzed successfully. Here is the feedback from our AI system:</p>" +
                               $"<p><strong>Match Score:</strong> {result.MatchScore}%</p>" +
                               $"<p><strong>Critique:</strong> {result.Critique}</p>" +
                               $"<p><strong>Skills & Strengths:</strong></p>" +
                               $"<ul>" +
                               string.Join("", result.SkillsStrengths.Select(s => $"<li>{s}</li>")) +
                               $"</ul>" +
                               $"<p><strong>Suggested Courses:</strong></p>" +
                               $"<ul>" +
                               string.Join("", result.SuggestedCourses.Select(c => $"<li>{c}</li>")) +
                               $"</ul>" +
                               $"<br/><p>Keep learning and building!</p>" +
                               $"<p>Best regards,<br/>AITraining System</p>";

                    await _notificationService.SendEmailAsync(user.Email, subject, body);
                    await _notificationService.CreateInAppNotificationAsync(
                        userId,
                        "Resume Analysis Complete",
                        $"AI has completed analyzing your resume. Critique: {(result.Critique.Length > 100 ? result.Critique.Substring(0, 97) + "..." : result.Critique)}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send resume analysis email/notification.");
            }
        }

        private async Task<string> CallOpenAIApiAsync(string prompt, string systemMessage = "", bool jsonMode = false)
        {
            var messages = new List<ChatMessageDto>();
            if (!string.IsNullOrEmpty(systemMessage))
            {
                messages.Add(new ChatMessageDto { Role = "system", Content = systemMessage });
            }
            messages.Add(new ChatMessageDto { Role = "user", Content = prompt });

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var payloadJson = JsonSerializer.Serialize(messages, options);
            return await SendRawChatPayloadAsync(payloadJson, jsonMode);
        }

        private async Task<string> SendRawChatPayloadAsync(string messagesJson, bool jsonMode)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY")
            {
                throw new Exception("OpenAI API key not configured.");
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var payload = new Dictionary<string, object>
            {
                { "model", _model },
                { "messages", JsonDocument.Parse(messagesJson).RootElement }
            };

            if (jsonMode)
            {
                payload.Add("response_format", new { type = "json_object" });
            }

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            requestMessage.Content = content;

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API Error {response.StatusCode}: {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return text ?? string.Empty;
        }

        private QuizDto GetMockFallbackQuiz(Guid courseId, string topic)
        {
            return new QuizDto
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = $"Fundamentals of {topic}",
                Description = $"Assess your knowledge of {topic} through multiple choice questions.",
                PassingScore = 70,
                IsFinal = false,
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Id = Guid.NewGuid(),
                        Text = $"What is a primary benefit of using {topic} in enterprise systems?",
                        Points = 10,
                        QuestionType = "SingleChoice",
                        Options = new List<OptionDto>
                        {
                            new OptionDto { Id = Guid.NewGuid(), OptionText = "Improved scalability and execution flow", IsCorrect = true },
                            new OptionDto { Id = Guid.NewGuid(), OptionText = "Reduced system development cost", IsCorrect = false },
                            new OptionDto { Id = Guid.NewGuid(), OptionText = "No system maintenance required", IsCorrect = false }
                        }
                    },
                    new QuestionDto
                    {
                        Id = Guid.NewGuid(),
                        Text = $"Which component in {topic} handles orchestration and integration?",
                        Points = 10,
                        QuestionType = "SingleChoice",
                        Options = new List<OptionDto>
                        {
                            new OptionDto { Id = Guid.NewGuid(), OptionText = "Message queues and controllers", IsCorrect = true },
                            new OptionDto { Id = Guid.NewGuid(), OptionText = "Database storage drivers", IsCorrect = false },
                            new OptionDto { Id = Guid.NewGuid(), OptionText = "Browser user interfaces", IsCorrect = false }
                        }
                    }
                }
            };
        }

        public async Task<ApiResponse<ExtendedMockInterviewResponseDto>> StartMockInterviewAsync(Guid userId, StartMockInterviewDto request)
        {
            var sessionId = Guid.NewGuid().ToString();

            // Build RAG System Prompt
            var systemPromptBuilder = new StringBuilder();
            systemPromptBuilder.AppendLine($"You are an expert technical interviewer conducting a mock interview on the topic of: {request.CourseTopic}.");
            systemPromptBuilder.AppendLine($"Difficulty Level: {request.Difficulty}. Target language: {request.Language}.");
            systemPromptBuilder.AppendLine("Engage with the candidate in a highly professional, interactive step-by-step manner.");
            systemPromptBuilder.AppendLine($"Ask exactly one question at a time. The interview will have exactly {request.QuestionCount} questions.");

            if (!string.IsNullOrEmpty(request.ResumeText))
            {
                systemPromptBuilder.AppendLine("\nCandidate's Resume Context:");
                systemPromptBuilder.AppendLine(request.ResumeText);
                systemPromptBuilder.AppendLine("Analyze the candidate's projects and technical skills. Tailor your questions specifically to test the depths of their claimed experiences and context.");
            }

            if (!string.IsNullOrEmpty(request.JobDescriptionText))
            {
                systemPromptBuilder.AppendLine("\nTarget Job Description (JD):");
                systemPromptBuilder.AppendLine(request.JobDescriptionText);
                systemPromptBuilder.AppendLine("Align the complexity and themes of your questions to match what is requested in this Job Description.");
            }

            systemPromptBuilder.AppendLine("\nReturn the response strictly formatted as a valid JSON object matching this schema:");
            systemPromptBuilder.AppendLine("{");
            systemPromptBuilder.AppendLine("  \"nextQuestionOrFeedback\": \"The interview question or introductory remarks\",");
            systemPromptBuilder.AppendLine("  \"hints\": [\"keyword 1\", \"keyword 2\", \"keyword 3\"] // Generate 3 key technical keywords or concepts the candidate should address in their answer");
            systemPromptBuilder.AppendLine("}");

            var messages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "system", Content = systemPromptBuilder.ToString() },
                new ChatMessageDto { Role = "user", Content = $"Please greet the candidate and state the first technical question for {request.CourseTopic}." }
            };

            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var payloadJson = JsonSerializer.Serialize(messages, options);
                var responseJson = await SendRawChatPayloadAsync(payloadJson, true);

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var questionText = root.GetProperty("nextQuestionOrFeedback").GetString() ?? string.Empty;
                var hints = new List<string>();
                if (root.TryGetProperty("hints", out var hEl))
                {
                    foreach (var item in hEl.EnumerateArray())
                    {
                        hints.Add(item.GetString() ?? string.Empty);
                    }
                }

                // Persist session to Database
                var newSession = new MockInterviewSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SessionId = sessionId,
                    CourseTopic = request.CourseTopic,
                    ConfigSettings = JsonSerializer.Serialize(request),
                    QuestionByQuestionLogsJson = JsonSerializer.Serialize(new List<object> {
                        new {
                            question = questionText,
                            answer = (string?)null,
                            hints = hints
                        }
                    }),
                    SpeechAnalyticsJson = "{}",
                    BehavioralAnalyticsJson = "{}",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    _dbContext.MockInterviewSessions.Add(newSession);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database persistence failed when starting mock interview session.");
                }

                var response = new ExtendedMockInterviewResponseDto
                {
                    SessionId = sessionId,
                    NextQuestionOrFeedback = questionText,
                    IsFinished = false,
                    Hints = hints
                };

                return ApiResponse<ExtendedMockInterviewResponseDto>.SuccessResponse(response, "Mock interview session started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start stateful mock interview. Using fallback first question.");
                
                var fallbackSession = new MockInterviewSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SessionId = sessionId,
                    CourseTopic = request.CourseTopic,
                    ConfigSettings = JsonSerializer.Serialize(request),
                    QuestionByQuestionLogsJson = JsonSerializer.Serialize(new List<object> {
                        new {
                            question = $"Welcome to your interview on {request.CourseTopic}. Can you explain your experience with this technology and describe a recent project you built?",
                            answer = (string?)null,
                            hints = new List<string> { "architecture", "scalability", "experience" }
                        }
                    }),
                    SpeechAnalyticsJson = "{}",
                    BehavioralAnalyticsJson = "{}",
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    _dbContext.MockInterviewSessions.Add(fallbackSession);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database persistence failed when starting mock interview session (fallback path).");
                }

                return ApiResponse<ExtendedMockInterviewResponseDto>.SuccessResponse(new ExtendedMockInterviewResponseDto
                {
                    SessionId = sessionId,
                    NextQuestionOrFeedback = $"Welcome to your interview on {request.CourseTopic}. Can you explain your experience with this technology and describe a recent project you built?",
                    IsFinished = false,
                    Hints = new List<string> { "architecture", "scalability", "experience" }
                }, "Started interview with fallback.");
            }
        }

        public async Task<ApiResponse<ExtendedMockInterviewResponseDto>> SubmitMockInterviewStepAsync(Guid userId, SubmitMockInterviewStepDto request)
        {
            var session = await _dbContext.MockInterviewSessions
                .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.UserId == userId);

            if (session == null)
            {
                return ApiResponse<ExtendedMockInterviewResponseDto>.FailResponse("Interview session not found.");
            }

            var config = JsonSerializer.Deserialize<StartMockInterviewDto>(session.ConfigSettings) ?? new StartMockInterviewDto();
            
            // Deserialize previous logs
            var logs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(session.QuestionByQuestionLogsJson) ?? new List<Dictionary<string, object>>();

            // Update the last question with the student's answer
            if (logs.Count > 0)
            {
                logs[logs.Count - 1]["answer"] = request.StudentAnswer ?? string.Empty;
            }

            // Update edge metrics aggregations
            UpdateSessionEdgeMetrics(session, request, logs.Count);

            var isLastQuestion = logs.Count >= config.QuestionCount || request.ForceFinish;

            if (isLastQuestion)
            {
                // Extract session tracking metrics for prompt grounding and evaluation consistency
                int slouchCount = 0;
                int tabSwitches = 0;
                try
                {
                    var behavior = string.IsNullOrEmpty(session.BehavioralAnalyticsJson) || session.BehavioralAnalyticsJson == "{}"
                        ? new Dictionary<string, object>()
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(session.BehavioralAnalyticsJson);
                    if (behavior != null)
                    {
                        if (behavior.TryGetValue("slouchCount", out var sVal)) slouchCount = Convert.ToInt32(sVal.ToString());
                        if (behavior.TryGetValue("tabSwitches", out var tVal)) tabSwitches = Convert.ToInt32(tVal.ToString());
                    }
                }
                catch { }

                int fillerWordsCount = 0;
                try
                {
                    var speech = string.IsNullOrEmpty(session.SpeechAnalyticsJson) || session.SpeechAnalyticsJson == "{}"
                        ? new Dictionary<string, object>()
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(session.SpeechAnalyticsJson);
                    if (speech != null && speech.TryGetValue("fillerWords", out var fVal) && fVal != null)
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(fVal.ToString());
                        if (list != null) fillerWordsCount = list.Count;
                    }
                }
                catch { }

                // Conclude interview and get final evaluation
                var evaluationPrompt = new StringBuilder();
                if (request.ForceFinish)
                {
                    evaluationPrompt.AppendLine($"The candidate chose to end the interview early after answering {logs.Count} questions.");
                }
                else
                {
                    evaluationPrompt.AppendLine($"The candidate has answered all {config.QuestionCount} questions.");
                }
                evaluationPrompt.AppendLine("Here is the transcript of the interview:");
                foreach (var log in logs)
                {
                    evaluationPrompt.AppendLine($"Q: {log["question"]}");
                    evaluationPrompt.AppendLine($"A: {log.GetValueOrDefault("answer")}");
                }

                evaluationPrompt.AppendLine("\nHere are the physical and speech dynamics metrics tracked during the session:");
                evaluationPrompt.AppendLine($"- Average Eye Contact Rate: {session.EyeContactPercentage}%");
                evaluationPrompt.AppendLine($"- Body Posture Slouch Count: {slouchCount}");
                evaluationPrompt.AppendLine($"- Tab Switching Incidents: {tabSwitches}");
                evaluationPrompt.AppendLine($"- Filler Words Count: {fillerWordsCount}");

                evaluationPrompt.AppendLine("\nPerform a comprehensive evaluation and return a valid JSON object matching this schema:");
                evaluationPrompt.AppendLine("{");
                evaluationPrompt.AppendLine("  \"overallScore\": 85, // Weighted average of the five sub-scores");
                evaluationPrompt.AppendLine("  \"technicalScore\": 85, // Integer 0-100");
                evaluationPrompt.AppendLine("  \"communicationScore\": 85, // Integer 0-100");
                evaluationPrompt.AppendLine("  \"confidenceScore\": 85, // Integer 50-100, deduct points from 95 for excessive filler words or volume variance issues");
                evaluationPrompt.AppendLine("  \"grammarScore\": 85, // Integer 0-100");
                evaluationPrompt.AppendLine("  \"bodyLanguageScore\": 85, // Integer 50-100, based on eye contact, slouch count, and tab switching");
                evaluationPrompt.AppendLine("  \"feedback\": \"Detailed markdown formatting report with feedback, strengths, and areas to improve.\",");
                evaluationPrompt.AppendLine("  \"nextQuestionOrFeedback\": \"Conclude the interview with a final statement\"");
                evaluationPrompt.AppendLine("}");

                var messages = new List<ChatMessageDto>
                {
                    new ChatMessageDto { Role = "system", Content = $"You are evaluating a software developer candidate's mock interview on {session.CourseTopic}." },
                    new ChatMessageDto { Role = "user", Content = evaluationPrompt.ToString() }
                };

                try
                {
                    var responseJson = await CallOpenAIApiAsync(evaluationPrompt.ToString(), "You are an interview evaluator returning raw JSON.", true);
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    session.TechnicalScore = root.GetProperty("technicalScore").GetInt32();
                    session.CommunicationScore = root.GetProperty("communicationScore").GetInt32();
                    session.ConfidenceScore = root.GetProperty("confidenceScore").GetInt32();
                    session.GrammarScore = root.GetProperty("grammarScore").GetInt32();
                    session.BodyLanguageScore = root.GetProperty("bodyLanguageScore").GetInt32();

                    // Mathematically enforce the weighted average for the overall score
                    session.OverallScore = (int)((session.TechnicalScore * 0.40) + 
                                                 (session.CommunicationScore * 0.20) + 
                                                 (session.ConfidenceScore * 0.15) + 
                                                 (session.GrammarScore * 0.15) + 
                                                 (session.BodyLanguageScore * 0.10));
                    
                    var finalFeedback = root.GetProperty("feedback").GetString() ?? string.Empty;
                    var conclusionText = root.GetProperty("nextQuestionOrFeedback").GetString() ?? string.Empty;

                    session.QuestionByQuestionLogsJson = JsonSerializer.Serialize(logs);
                    session.IsCompleted = true;

                    try
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Database persistence failed when concluding mock interview step.");
                    }

                    return ApiResponse<ExtendedMockInterviewResponseDto>.SuccessResponse(new ExtendedMockInterviewResponseDto
                    {
                        SessionId = session.SessionId,
                        NextQuestionOrFeedback = conclusionText,
                        Score = session.OverallScore,
                        Feedback = finalFeedback,
                        IsFinished = true
                    }, "Interview finished and evaluated.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed final evaluation. Using fallback scorecard values.");
                    session.OverallScore = 81;
                    session.TechnicalScore = 82;
                    session.CommunicationScore = 78;
                    session.ConfidenceScore = 80;
                    session.GrammarScore = 85;
                    session.BodyLanguageScore = 80;
                    session.IsCompleted = true;
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Database persistence failed when concluding mock interview step (fallback evaluation).");
                    }

                    return ApiResponse<ExtendedMockInterviewResponseDto>.SuccessResponse(new ExtendedMockInterviewResponseDto
                    {
                        SessionId = session.SessionId,
                        NextQuestionOrFeedback = "Thank you, this concludes the interview. Here is your evaluation:",
                        Score = 80,
                        Feedback = "Good overall performance. Focused technical answers with minor hesitation.",
                        IsFinished = true
                    }, "Interview finished with fallback evaluation.");
                }
            }
            else
            {
                // Generate next question with full RAG resume/JD context and spoken tone constraints
                var stepPromptBuilder = new System.Text.StringBuilder();
                stepPromptBuilder.AppendLine($"You are conducting a professional technical mock interview on the topic of: {session.CourseTopic}.");
                stepPromptBuilder.AppendLine($"Difficulty Level: {config.Difficulty}. Mode: {config.Mode}. Target language: {config.Language}.");
                stepPromptBuilder.AppendLine("Engage with the candidate in a highly professional, interactive step-by-step manner.");
                stepPromptBuilder.AppendLine($"Ask exactly one question at a time. The interview will have exactly {config.QuestionCount} questions. Do not say goodbye or conclude yet.");
                stepPromptBuilder.AppendLine("IMPORTANT: The candidate hears your response via browser Text-to-Speech synthesis. Write your response in a natural, conversational, spoken tone. Avoid raw markdown tables, lists of bullets, or code dumps unless explicitly asked. Frame questions naturally as a human interviewer would.");

                if (!string.IsNullOrEmpty(config.ResumeText))
                {
                    stepPromptBuilder.AppendLine("\nCandidate's Resume Context:");
                    stepPromptBuilder.AppendLine(config.ResumeText);
                    stepPromptBuilder.AppendLine("Tailor your questions specifically to test the depths of their claimed experiences and technical skills.");
                }

                if (!string.IsNullOrEmpty(config.JobDescriptionText))
                {
                    stepPromptBuilder.AppendLine("\nTarget Job Description (JD):");
                    stepPromptBuilder.AppendLine(config.JobDescriptionText);
                    stepPromptBuilder.AppendLine("Align themes and complexity of your questions to match what is requested in this Job Description.");
                }

                stepPromptBuilder.AppendLine("\nReturn the response strictly formatted as a valid JSON object matching this schema:");
                stepPromptBuilder.AppendLine("{");
                stepPromptBuilder.AppendLine("  \"nextQuestionOrFeedback\": \"Critique of previous answer and the next interview question\",");
                stepPromptBuilder.AppendLine("  \"hints\": [\"keyword 1\", \"keyword 2\", \"keyword 3\"] // 3 key concepts/terms they should address");
                stepPromptBuilder.AppendLine("}");

                var chatMessages = new List<ChatMessageDto>
                {
                    new ChatMessageDto { Role = "system", Content = stepPromptBuilder.ToString() }
                };

                // Add historical questions and answers
                foreach (var log in logs)
                {
                    chatMessages.Add(new ChatMessageDto { Role = "assistant", Content = log["question"]?.ToString() ?? string.Empty });
                    if (log.TryGetValue("answer", out var ans) && ans != null)
                    {
                        chatMessages.Add(new ChatMessageDto { Role = "user", Content = ans.ToString() ?? string.Empty });
                    }
                }

                // Adaptive Follow-up Logic: Check if the user mentioned something worth probing
                bool shouldInjectFollowUp = CheckForAdaptiveFollowUp(request.StudentAnswer, logs);
                if (shouldInjectFollowUp)
                {
                    chatMessages.Add(new ChatMessageDto { Role = "system", Content = "ALERT: The candidate gave an interesting but brief answer. Follow up specifically on their mentioned detail instead of the standard question stack." });
                }

                try
                {
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var payloadJson = JsonSerializer.Serialize(chatMessages, options);
                    var responseJson = await SendRawChatPayloadAsync(payloadJson, true);

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    var nextQuestion = root.GetProperty("nextQuestionOrFeedback").GetString() ?? string.Empty;
                    var hints = new List<string>();
                    if (root.TryGetProperty("hints", out var hEl))
                    {
                        foreach (var item in hEl.EnumerateArray())
                        {
                            hints.Add(item.GetString() ?? string.Empty);
                        }
                    }

                    // Add new log entry for the next question
                    logs.Add(new Dictionary<string, object>
                    {
                        { "question", nextQuestion },
                        { "answer", null! },
                        { "hints", hints }
                    });

                    session.QuestionByQuestionLogsJson = JsonSerializer.Serialize(logs);
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Database persistence failed when saving next mock interview question.");
                    }

                    return ApiResponse<ExtendedMockInterviewResponseDto>.SuccessResponse(new ExtendedMockInterviewResponseDto
                    {
                        SessionId = session.SessionId,
                        NextQuestionOrFeedback = nextQuestion,
                        IsFinished = false,
                        Hints = hints
                    }, "Advanced step processed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed step call. Using fallback next question.");
                    var fallbackQuestion = "Understood. How would you design a scalable microservices structure to handle high-traffic spike events?";
                    var fallbackHints = new List<string> { "load balancing", "caching", "message queue" };

                    logs.Add(new Dictionary<string, object>
                    {
                        { "question", fallbackQuestion },
                        { "answer", null! },
                        { "hints", fallbackHints }
                    });

                    session.QuestionByQuestionLogsJson = JsonSerializer.Serialize(logs);
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "Database persistence failed when saving next mock interview question (fallback).");
                    }

                    return ApiResponse<ExtendedMockInterviewResponseDto>.SuccessResponse(new ExtendedMockInterviewResponseDto
                    {
                        SessionId = session.SessionId,
                        NextQuestionOrFeedback = fallbackQuestion,
                        IsFinished = false,
                        Hints = fallbackHints
                    }, "Step processed with fallback.");
                }
            }
        }

        private bool CheckForAdaptiveFollowUp(string? answer, List<Dictionary<string, object>> logs)
        {
            if (string.IsNullOrEmpty(answer)) return false;
            var words = answer.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 40) return false; // Already comprehensive

            // Scan for vague/ambiguous project terms
            var keywords = new[] { "migrate", "migration", "redesigned", "optimized", "bottleneck", "re-architected", "custom tool", "legacy", "security patch" };
            return keywords.Any(k => answer.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateSessionEdgeMetrics(MockInterviewSession session, SubmitMockInterviewStepDto request, int turnCount)
        {
            // Update average eye contact
            var existingEyeContact = session.EyeContactPercentage;
            if (existingEyeContact == 0)
            {
                session.EyeContactPercentage = (int)(request.EyeContactRate * 100);
            }
            else
            {
                session.EyeContactPercentage = (int)((existingEyeContact * (turnCount - 1) + (request.EyeContactRate * 100)) / turnCount);
            }

            // Speech Analytics Json Update
            var speech = string.IsNullOrEmpty(session.SpeechAnalyticsJson) || session.SpeechAnalyticsJson == "{}"
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(session.SpeechAnalyticsJson) ?? new Dictionary<string, object>();

            var wpmList = speech.TryGetValue("wpmHistory", out var wEl) && wEl != null
                ? JsonSerializer.Deserialize<List<int>>(JsonSerializer.Serialize(wEl)) ?? new List<int>()
                : new List<int>();
            
            var wpm = request.WordCount; // Word count in this turn
            wpmList.Add(wpm);
            speech["wpmHistory"] = wpmList;

            var fillerList = speech.TryGetValue("fillerWords", out var fEl) && fEl != null
                ? JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(fEl)) ?? new List<string>()
                : new List<string>();
            fillerList.AddRange(request.FillerWords);
            speech["fillerWords"] = fillerList;

            var pauseCount = speech.TryGetValue("pauseCount", out var pEl) ? Convert.ToInt32(pEl.ToString()) : 0;
            if (request.VolumeVariance > 15) // simple threshold for volume variance indicating pauses
            {
                pauseCount += 1;
            }
            speech["pauseCount"] = pauseCount;

            session.SpeechAnalyticsJson = JsonSerializer.Serialize(speech);

            // Behavioral Analytics Json Update
            var behavior = string.IsNullOrEmpty(session.BehavioralAnalyticsJson) || session.BehavioralAnalyticsJson == "{}"
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(session.BehavioralAnalyticsJson) ?? new Dictionary<string, object>();

            var slouchCount = behavior.TryGetValue("slouchCount", out var sEl) ? Convert.ToInt32(sEl.ToString()) : 0;
            slouchCount += request.SlouchCount;
            behavior["slouchCount"] = slouchCount;

            var tabSwitches = behavior.TryGetValue("tabSwitches", out var tEl) ? Convert.ToInt32(tEl.ToString()) : 0;
            if (request.TabSwitched)
            {
                tabSwitches += 1;
            }
            behavior["tabSwitches"] = tabSwitches;

            var emotions = behavior.TryGetValue("detectedEmotions", out var emoEl) && emoEl != null
                ? JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(emoEl)) ?? new List<string>()
                : new List<string>();
            emotions.AddRange(request.DetectedEmotions);
            behavior["detectedEmotions"] = emotions;

            session.BehavioralAnalyticsJson = JsonSerializer.Serialize(behavior);
        }

        public async Task<ApiResponse<IEnumerable<MockInterviewSessionListItemDto>>> GetMockInterviewHistoryAsync(Guid userId)
        {
            var sessions = await _dbContext.MockInterviewSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new MockInterviewSessionListItemDto
                {
                    Id = s.Id,
                    SessionId = s.SessionId,
                    CourseTopic = s.CourseTopic,
                    OverallScore = s.OverallScore,
                    IsCompleted = s.IsCompleted,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<IEnumerable<MockInterviewSessionListItemDto>>.SuccessResponse(sessions, "Historical sessions retrieved.");
        }

        public async Task<ApiResponse<MockInterviewScorecardDto>> GetMockInterviewScorecardAsync(Guid userId, Guid id)
        {
            var s = await _dbContext.MockInterviewSessions
                .FirstOrDefaultAsync(session => session.Id == id && (userId == Guid.Empty || session.UserId == userId));

            if (s == null)
            {
                return ApiResponse<MockInterviewScorecardDto>.FailResponse("Scorecard not found.");
            }

            var scorecard = new MockInterviewScorecardDto
            {
                Id = s.Id,
                SessionId = s.SessionId,
                CourseTopic = s.CourseTopic,
                ConfigSettings = s.ConfigSettings,
                OverallScore = s.OverallScore,
                CommunicationScore = s.CommunicationScore,
                TechnicalScore = s.TechnicalScore,
                ConfidenceScore = s.ConfidenceScore,
                GrammarScore = s.GrammarScore,
                EyeContactPercentage = s.EyeContactPercentage,
                BodyLanguageScore = s.BodyLanguageScore,
                SpeechAnalyticsJson = s.SpeechAnalyticsJson,
                BehavioralAnalyticsJson = s.BehavioralAnalyticsJson,
                QuestionByQuestionLogsJson = s.QuestionByQuestionLogsJson,
                VideoReplayUrl = s.VideoReplayUrl,
                CreatedAt = s.CreatedAt
            };

            return ApiResponse<MockInterviewScorecardDto>.SuccessResponse(scorecard, "Scorecard retrieved successfully.");
        }

        public async Task<ApiResponse<string>> UploadInterviewVideoAsync(Guid userId, Guid id, IFormFile file)
        {
            var session = await _dbContext.MockInterviewSessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return ApiResponse<string>.FailResponse("Session not found.");
            }

            if (file == null || file.Length == 0)
            {
                return ApiResponse<string>.FailResponse("Invalid video file.");
            }

            using var stream = file.OpenReadStream();
            var fileName = $"replay_{id}_{Guid.NewGuid():N}.mp4";
            var url = await _storageService.UploadFileAsync(stream, fileName, "interviews", file.ContentType);

            session.VideoReplayUrl = url;
            await _dbContext.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse(url, "Webcam replay video uploaded successfully.");
        }

        public async Task<ApiResponse<IEnumerable<AdminMockInterviewSessionDto>>> GetAllMockInterviewsAsync()
        {
            var sessions = await _dbContext.MockInterviewSessions
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new AdminMockInterviewSessionDto
                {
                    Id = s.Id,
                    SessionId = s.SessionId,
                    CourseTopic = s.CourseTopic,
                    CandidateName = s.User != null ? s.User.FullName : "Unknown",
                    CandidateEmail = s.User != null ? s.User.Email : "Unknown",
                    OverallScore = s.OverallScore,
                    IsCompleted = s.IsCompleted,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<IEnumerable<AdminMockInterviewSessionDto>>.SuccessResponse(sessions, "All mock interview sessions retrieved.");
        }
    }
}
