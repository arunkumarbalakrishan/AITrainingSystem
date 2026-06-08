using System;
using System.Threading.Tasks;
using AITrainingSystem.Application.Common.Models;
using AITrainingSystem.Application.DTOs.Payment;

namespace AITrainingSystem.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<ApiResponse<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(Guid userId, CreateCheckoutRequestDto dto);
        Task<ApiResponse<bool>> ProcessWebhookAsync(string payload, string stripeSignatureHeader);
        Task<ApiResponse<bool>> ConfirmMockPaymentAsync(Guid userId, Guid courseId);
    }
}
