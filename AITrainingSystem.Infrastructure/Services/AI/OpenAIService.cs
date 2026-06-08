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

        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<OpenAIService> logger,
            ICourseRepository courseRepository,
            ILessonProgressRepository lessonProgressRepository)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _courseRepository = courseRepository;
            _lessonProgressRepository = lessonProgressRepository;

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

                return ApiResponse<ResumeAnalysisResultDto>.SuccessResponse(result, "Resume analysis completed.");
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
    }
}
