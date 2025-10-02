using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace WhatsApp.Infrastructure.HealthChecks;

public class BaileysServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BaileysServiceHealthCheck> _logger;

    public BaileysServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BaileysServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceUrl = _configuration["BaileysService:Url"] ?? "http://localhost:3000";
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await httpClient.GetAsync($"{serviceUrl}/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return HealthCheckResult.Healthy(
                    "Baileys service is running and responsive",
                    new Dictionary<string, object>
                    {
                        { "url", serviceUrl },
                        { "response", content }
                    });
            }

            return HealthCheckResult.Degraded(
                $"Baileys service returned status code: {response.StatusCode}",
                data: new Dictionary<string, object>
                {
                    { "url", serviceUrl },
                    { "statusCode", (int)response.StatusCode }
                });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to reach Baileys service");
            return HealthCheckResult.Unhealthy(
                "Baileys service is not reachable",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Baileys service health check timed out");
            return HealthCheckResult.Unhealthy(
                "Baileys service health check timed out",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Baileys service health check");
            return HealthCheckResult.Unhealthy(
                "Unexpected error during health check",
                ex);
        }
    }
}
