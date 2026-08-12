using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;

namespace AITrainingSystem.API.HealthChecks;

public class StripeHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public StripeHealthCheck(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var secretKey = _config["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Equals("YOUR_STRIPE_SECRET_KEY"))
            {
                return HealthCheckResult.Degraded("Stripe API integration keys are not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.stripe.com/v1/balance");
            request.Headers.Add("Authorization", $"Bearer {secretKey}");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(3000); // 3 seconds timeout

            var response = await client.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Stripe API gateway connection verified successfully.");
            }

            return HealthCheckResult.Degraded($"Stripe API responded with code {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Stripe gateway monitoring status check threw exception: {ex.Message}");
        }
    }
}
