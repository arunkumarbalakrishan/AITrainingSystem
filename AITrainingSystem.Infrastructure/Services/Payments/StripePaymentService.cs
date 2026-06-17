using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Payment;
using AITrainingSystem.Application.Interfaces.Repositories;
using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace AITrainingSystem.Infrastructure.Services.Payments
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IUserRepository _userRepo;
        private readonly IEnrollmentService _enrollmentService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(
            IPaymentRepository paymentRepo,
            ICourseRepository courseRepo,
            IUserRepository userRepo,
            IEnrollmentService enrollmentService,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<StripePaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _courseRepo = courseRepo;
            _userRepo = userRepo;
            _enrollmentService = enrollmentService;
            _notificationService = notificationService;
            _configuration = configuration;
            _logger = logger;

            // Initialize Stripe API Key
            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(stripeSecretKey))
            {
                StripeConfiguration.ApiKey = stripeSecretKey;
            }
        }

        public async Task<ApiResponse<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(Guid userId, CreateCheckoutRequestDto dto)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<CheckoutSessionResponseDto>.FailResponse("User not found.");
            }

            var course = await _courseRepo.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                return ApiResponse<CheckoutSessionResponseDto>.FailResponse("Course not found.");
            }

            // Check if Stripe is configured
            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey) || stripeSecretKey == "YOUR_STRIPE_SECRET_KEY")
            {
                // Fallback to Mock checkout URL
                _logger.LogWarning("Stripe SecretKey missing. Generating mock checkout session.");
                var mockSessionId = $"mock_session_{Guid.NewGuid()}";

                var mockPayment = new Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CourseId = dto.CourseId,
                    Amount = course.Price,
                    Currency = "USD",
                    Status = "Pending",
                    StripeSessionId = mockSessionId,
                    CreatedAt = DateTime.UtcNow
                };

                await _paymentRepo.CreateAsync(mockPayment);
                await _paymentRepo.SaveChangesAsync();

                return ApiResponse<CheckoutSessionResponseDto>.SuccessResponse(new CheckoutSessionResponseDto
                {
                    SessionId = mockSessionId,
                    CheckoutUrl = $"/api/payment/confirm?courseId={dto.CourseId}"
                }, "Mock Checkout session created successfully.");
            }

            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(course.Price * 100), // Stripe expects amounts in cents
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = course.Title,
                                    Description = course.Description
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = dto.SuccessUrl,
                    CancelUrl = dto.CancelUrl,
                    CustomerEmail = user.Email
                };

                var service = new SessionService();
                Session session = await service.CreateAsync(options);

                // Save Payment Record as Pending
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CourseId = dto.CourseId,
                    Amount = course.Price,
                    Currency = "USD",
                    Status = "Pending",
                    StripeSessionId = session.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _paymentRepo.CreateAsync(payment);
                await _paymentRepo.SaveChangesAsync();

                return ApiResponse<CheckoutSessionResponseDto>.SuccessResponse(new CheckoutSessionResponseDto
                {
                    SessionId = session.Id,
                    CheckoutUrl = session.Url
                }, "Stripe Checkout session created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Checkout session creation failed.");
                return ApiResponse<CheckoutSessionResponseDto>.FailResponse($"Stripe Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ProcessWebhookAsync(string payload, string stripeSignatureHeader)
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                return ApiResponse<bool>.FailResponse("Stripe Webhook Secret not configured.");
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(payload, stripeSignatureHeader, webhookSecret);

                if (stripeEvent.Type == Events.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        var payment = await _paymentRepo.GetByStripeSessionIdAsync(session.Id);
                        if (payment != null && payment.Status == "Pending")
                        {
                            payment.Status = "Completed";
                            payment.StripePaymentIntentId = session.PaymentIntentId;
                            payment.UpdatedAt = DateTime.UtcNow;

                            await _paymentRepo.UpdateAsync(payment);
                            await _paymentRepo.SaveChangesAsync();

                            // Enroll User in course
                            await _enrollmentService.EnrollUserAsync(payment.UserId, payment.CourseId);

                            // Send alerts
                            await _notificationService.CreateInAppNotificationAsync(
                                payment.UserId,
                                "Enrollment Confirmed",
                                $"Welcome! You have been successfully enrolled in the course."
                            );

                            var user = await _userRepo.GetByIdAsync(payment.UserId);
                            if (user != null)
                            {
                                await _notificationService.SendEmailAsync(
                                    user.Email,
                                    "Enrollment Confirmed",
                                    $"Hi {user.FullName},<br/><br/>Your payment was successful and you are now enrolled."
                                );
                            }
                        }
                    }
                }

                return ApiResponse<bool>.SuccessResponse(true, "Webhook processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing Stripe webhook.");
                return ApiResponse<bool>.FailResponse($"Webhook Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ConfirmMockPaymentAsync(Guid userId, Guid courseId)
        {
            _logger.LogInformation("Processing Mock Payment confirmation for user {UserId} and course {CourseId}", userId, courseId);
            
            try
            {
                await _enrollmentService.EnrollUserAsync(userId, courseId);

                var course = await _courseRepo.GetByIdAsync(courseId);
                var amount = course != null ? course.Price : 0;

                // Create mock payment record if not exists
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CourseId = courseId,
                    Amount = amount,
                    Currency = "USD",
                    Status = "Completed",
                    StripeSessionId = $"mock_success_{Guid.NewGuid()}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _paymentRepo.CreateAsync(payment);
                await _paymentRepo.SaveChangesAsync();

                // Send notification alerts
                await _notificationService.CreateInAppNotificationAsync(
                    userId,
                    "Enrollment Confirmed (Mock)",
                    $"Welcome! You have been successfully enrolled in the course via Mock Checkout."
                );

                var user = await _userRepo.GetByIdAsync(userId);
                if (user != null)
                {
                    await _notificationService.SendEmailAsync(
                        user.Email,
                        "Enrollment Confirmed (Mock)",
                        $"Hi {user.FullName},<br/><br/>Your mock payment was completed and you are enrolled."
                    );
                }

                return ApiResponse<bool>.SuccessResponse(true, "Mock payment confirmed, user enrolled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mock payment enrollment failed.");
                return ApiResponse<bool>.FailResponse($"Mock Enrollment Error: {ex.Message}");
            }
        }
    }
}
