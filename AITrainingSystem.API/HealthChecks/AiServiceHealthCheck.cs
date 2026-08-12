using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;

namespace AITrainingSystem.API.HealthChecks;

public class AiServiceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiServiceHealthCheck(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return HealthCheckResult.Degraded("OpenAI/Groq API key values are not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.groq.com/openai/v1/models");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(3000); // 3 seconds timeout

            var response = await client.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("AI service integration (Groq/OpenAI) is online and keys are active.");
            }

            return HealthCheckResult.Degraded($"AI service endpoint returned code {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"AI service check threw exception: {ex.Message}");
        }
    }
}
