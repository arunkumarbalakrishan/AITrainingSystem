using AITrainingSystem.Application.Features.Media.Interfaces;
using AITrainingSystem.Application.Features.Media.Services;
using AITrainingSystem.Application.Interfaces.Auth;
using AITrainingSystem.Application.Interfaces.DashboardService;
using AITrainingSystem.Application.Interfaces.Lessons;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Respository;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Application.Services;
using AITrainingSystem.Application.Validators.Course;
using AITrainingSystem.Infrastructure.Services;
using AITrainingSystem.Infrastructure.Services.AI;
using AITrainingSystem.Infrastructure.Services.Assessment;
using AITrainingSystem.Infrastructure.Services.Auth;
using AITrainingSystem.Infrastructure.Services.Courses;
using AITrainingSystem.Infrastructure.Services.Dashboard;
using AITrainingSystem.Infrastructure.Services.Email;
using AITrainingSystem.Infrastructure.Services.Enrollments;
using AITrainingSystem.Infrastructure.Services.Notes;
using AITrainingSystem.Infrastructure.Services.Payments;
using AITrainingSystem.Infrastructure.Services.Progress;
using AITrainingSystem.Infrastructure.Services.Storage;
using AITrainingSystem.Infrastructure.Services.Search;
using AITrainingSystem.Infrastructure.Services.users;
using AITrainingSystem.Persistence.Repositories;
using FluentValidation;


using AITrainingSystem.Infrastructure.Services.LiveClasses;

namespace AITrainingSystem.API.Extensions;

public static class DependencyInjectionExt
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Auth Services
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<JwtService>();


        // User Services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        
        // Course services
        services.AddValidatorsFromAssemblyContaining<CreateCourseValidator>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseService, CourseService>();

        //Lesson Sevices
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ILessonRepository, LessonRepository>();

        //Enrollment Services
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        // Media Services
        services.AddScoped<IMediaAccessService, MediaAccessService>();
        services.AddScoped<IMediaService, MediaService>();


        // Lesson Progress Services
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<ILessonProgressService, LessonProgressService>();

        // Progress Services
        services.AddScoped<IProgressService, ProgressService>();

        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ICertificateService, CertificateService>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICertificatePdfService, CertificatePdfService>();

        // New Repositories
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IAssessmentResultRepository, AssessmentResultRepository>();
        services.AddScoped<ILessonNoteRepository, LessonNoteRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();

        // New Services
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILessonProgressService, LessonProgressService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IRealTimeNotificationService, AITrainingSystem.API.Services.RealTimeNotificationService>();

        // Domain Services
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<ILessonNoteService, LessonNoteService>();
        services.AddScoped<INotificationService, SmtpNotificationService>();
        services.AddScoped<IPaymentService, StripePaymentService>();
        services.AddScoped<ILiveClassService, LiveClassService>();
        services.AddScoped<ILiveClassSessionService, AITrainingSystem.Infrastructure.Services.LiveClasses.LiveClassSessionService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        // Storage registration
        services.AddScoped<LocalStorageService>();
        services.AddScoped<CloudStorageService>();
        services.AddScoped<IStorageService>(sp =>
        {
            var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var provider = config["Storage:Provider"] ?? "Local";
            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<LocalStorageService>();
            }
            return sp.GetRequiredService<CloudStorageService>();
        });

        // Live Class Streaming Services
        services.AddScoped<IJitsiMeetingService, JitsiMeetingService>();
        services.AddHttpClient<IZoomApiService, ZoomApiService>();

        // AI Services with HttpClient
        services.AddHttpClient<IAIService, OpenAIService>();

        // Cache Services
        var redisConn = configuration["Redis:ConnectionString"];
        bool useRedis = false;
        StackExchange.Redis.ConnectionMultiplexer? connectionMultiplexer = null;
        if (!string.IsNullOrEmpty(redisConn))
        {
            try
            {
                var redisOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConn);
                redisOptions.ConnectTimeout = 1000; // 1 second connection check limit
                redisOptions.SyncTimeout = 1000;
                connectionMultiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(redisOptions);
                useRedis = connectionMultiplexer.IsConnected;
            }
            catch
            {
                useRedis = false;
                connectionMultiplexer?.Dispose();
                connectionMultiplexer = null;
            }
        }

        if (useRedis && connectionMultiplexer != null)
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(connectionMultiplexer);
            services.AddStackExchangeRedisCache(options =>
            {
                var redisOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConn);
                redisOptions.ConnectTimeout = 1000;
                redisOptions.SyncTimeout = 1000;
                options.ConfigurationOptions = redisOptions;
                options.InstanceName = "AITraining:";
            });
            services.AddScoped<ICacheService, AITrainingSystem.Infrastructure.Services.Cache.RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, AITrainingSystem.Infrastructure.Services.Cache.MemoryCacheService>();
        }

        // Background Queue Hosted Service
        services.AddHostedService<AITrainingSystem.Infrastructure.Services.Background.EmailQueueBackgroundService>();

        return services;
    }
}