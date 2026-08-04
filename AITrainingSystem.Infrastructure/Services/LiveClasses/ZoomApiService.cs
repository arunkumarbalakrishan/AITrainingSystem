using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AITrainingSystem.Application.DTOs.LiveClasses;
using AITrainingSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AITrainingSystem.Infrastructure.Services.LiveClasses
{
    public class ZoomApiService : IZoomApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ZoomApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ZoomMeetingResponseDto> CreateMeetingAsync(string topic, string description, DateTime startTime, int durationMinutes)
        {
            var accountId = _configuration["ZoomApi:AccountId"];
            var clientId = _configuration["ZoomApi:ClientId"];
            var clientSecret = _configuration["ZoomApi:ClientSecret"];

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return new ZoomMeetingResponseDto
                {
                    Success = false,
                    ErrorMessage = "Zoom API credentials (AccountId, ClientId, ClientSecret) are not configured in appsettings.json."
                };
            }

            try
            {
                // 1. Obtain Access Token via Server-to-Server OAuth
                var tokenUrl = $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={accountId}";
                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                var authBytes = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                var tokenResponse = await _httpClient.SendAsync(request);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errBody = await tokenResponse.Content.ReadAsStringAsync();
                    return new ZoomMeetingResponseDto
                    {
                        Success = false,
                        ErrorMessage = $"Failed to authenticate with Zoom API: {tokenResponse.StatusCode} - {errBody}"
                    };
                }

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                using var tokenDoc = JsonDocument.Parse(tokenJson);
                var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

                // 2. Create Zoom Meeting
                var createMeetingUrl = "https://api.zoom.us/v2/users/me/meetings";
                var meetingPayload = new
                {
                    topic = topic,
                    agenda = description,
                    type = 2, // Scheduled meeting
                    start_time = startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    duration = durationMinutes,
                    settings = new
                    {
                        host_video = true,
                        participant_video = true,
                        join_before_host = true,
                        mute_upon_entry = false,
                        waiting_room = false
                    }
                };

                var meetingRequest = new HttpRequestMessage(HttpMethod.Post, createMeetingUrl);
                meetingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                meetingRequest.Content = new StringContent(JsonSerializer.Serialize(meetingPayload), Encoding.UTF8, "application/json");

                var meetingResponse = await _httpClient.SendAsync(meetingRequest);
                if (!meetingResponse.IsSuccessStatusCode)
                {
                    var errBody = await meetingResponse.Content.ReadAsStringAsync();
                    return new ZoomMeetingResponseDto
                    {
                        Success = false,
                        ErrorMessage = $"Failed to create Zoom meeting: {meetingResponse.StatusCode} - {errBody}"
                    };
                }

                var meetingJson = await meetingResponse.Content.ReadAsStringAsync();
                using var meetingDoc = JsonDocument.Parse(meetingJson);
                var root = meetingDoc.RootElement;

                var joinUrl = root.GetProperty("join_url").GetString() ?? string.Empty;
                var startUrl = root.TryGetProperty("start_url", out var startEl) ? startEl.GetString() ?? joinUrl : joinUrl;
                var passcode = root.TryGetProperty("password", out var passEl) ? passEl.GetString() ?? string.Empty : string.Empty;
                var meetingId = root.GetProperty("id").GetInt64().ToString();

                return new ZoomMeetingResponseDto
                {
                    Success = true,
                    MeetingLink = joinUrl,
                    HostStartUrl = startUrl,
                    Passcode = passcode,
                    MeetingId = meetingId
                };
            }
            catch (Exception ex)
            {
                return new ZoomMeetingResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Zoom API Integration Exception: {ex.Message}"
                };
            }
        }
    }
}
