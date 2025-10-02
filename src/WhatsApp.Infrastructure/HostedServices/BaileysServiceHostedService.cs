using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace WhatsApp.Infrastructure.HostedServices;

public class BaileysServiceHostedService : BackgroundService
{
    private readonly ILogger<BaileysServiceHostedService> _logger;
    private readonly IConfiguration _configuration;
    private Process? _baileysProcess;
    private readonly string _baileysServicePath;
    private readonly int _healthCheckIntervalSeconds;
    private readonly int _maxRestartAttempts;
    private int _restartAttempts;
    private readonly HttpClient _httpClient;

    public BaileysServiceHostedService(
        ILogger<BaileysServiceHostedService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();

        // Get configuration
        var baileysPath = _configuration["BaileysService:Path"];
        var configuredPath = !string.IsNullOrEmpty(baileysPath)
            ? baileysPath
            : Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "baileys-service");

        // Convert to absolute path to ensure Process.Start works correctly
        _baileysServicePath = Path.GetFullPath(configuredPath);

        _healthCheckIntervalSeconds = _configuration.GetValue("BaileysService:HealthCheckIntervalSeconds", 30);
        _maxRestartAttempts = _configuration.GetValue("BaileysService:MaxRestartAttempts", 3);
        _restartAttempts = 0;

        _logger.LogInformation("BaileysServiceHostedService initialized with path: {Path}", _baileysServicePath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Baileys Service management...");

        try
        {
            // Wait a bit for the API to fully start
            await Task.Delay(2000, stoppingToken);

            // Start Baileys service
            await StartBaileysServiceAsync(stoppingToken);

            // Monitor health
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_healthCheckIntervalSeconds), stoppingToken);
                await CheckHealthAndRestartIfNeededAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Baileys Service management is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Baileys Service management");
        }
    }

    private async Task StartBaileysServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if baileys-service directory exists
            if (!Directory.Exists(_baileysServicePath))
            {
                _logger.LogError("Baileys service path not found: {Path}. Service will not start.", _baileysServicePath);
                _logger.LogInformation("Current directory: {CurrentDir}", Directory.GetCurrentDirectory());
                return;
            }

            _logger.LogInformation("Starting Baileys service from: {Path}", _baileysServicePath);

            // Use cmd.exe on Windows to properly execute npm commands
            var isWindows = OperatingSystem.IsWindows();
            var startInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "npm",
                Arguments = isWindows ? "/c npm run dev" : "run dev",
                WorkingDirectory = _baileysServicePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _baileysProcess = new Process { StartInfo = startInfo };

            // Subscribe to output events
            _baileysProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogDebug("[Baileys] {Output}", e.Data);
                }
            };

            _baileysProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogWarning("[Baileys Error] {Error}", e.Data);
                }
            };

            _baileysProcess.Start();
            _baileysProcess.BeginOutputReadLine();
            _baileysProcess.BeginErrorReadLine();

            _logger.LogInformation("Baileys service process started with PID: {ProcessId}", _baileysProcess.Id);

            // Wait for service to be ready
            await WaitForServiceReadyAsync(cancellationToken);

            _restartAttempts = 0; // Reset restart counter on successful start
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Baileys service");
            throw;
        }
    }

    private async Task WaitForServiceReadyAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = 30; // 30 seconds
        var attempt = 0;
        var serviceUrl = _configuration["BaileysService:Url"] ?? "http://localhost:3000";

        _logger.LogInformation("Waiting for Baileys service to be ready at {Url}...", serviceUrl);

        while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{serviceUrl}/health", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Baileys service is ready and healthy!");
                    return;
                }
            }
            catch
            {
                // Service not ready yet
            }

            attempt++;
            await Task.Delay(1000, cancellationToken);
        }

        if (attempt >= maxAttempts)
        {
            _logger.LogWarning("Baileys service did not respond to health check after {Seconds} seconds", maxAttempts);
        }
    }

    private async Task CheckHealthAndRestartIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_baileysProcess == null || _baileysProcess.HasExited)
        {
            _logger.LogWarning("Baileys service process has exited. Attempting restart...");
            await RestartBaileysServiceAsync(cancellationToken);
            return;
        }

        // Check health endpoint
        try
        {
            var serviceUrl = _configuration["BaileysService:Url"] ?? "http://localhost:3000";
            var response = await _httpClient.GetAsync($"{serviceUrl}/health", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Baileys service health check failed. Status: {StatusCode}", response.StatusCode);
                await RestartBaileysServiceAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Baileys service health check passed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reach Baileys service health endpoint. Attempting restart...");
            await RestartBaileysServiceAsync(cancellationToken);
        }
    }

    private async Task RestartBaileysServiceAsync(CancellationToken cancellationToken)
    {
        if (_restartAttempts >= _maxRestartAttempts)
        {
            _logger.LogError("Maximum restart attempts ({MaxAttempts}) reached. Giving up on Baileys service.", _maxRestartAttempts);
            return;
        }

        _restartAttempts++;
        _logger.LogInformation("Restart attempt {Attempt} of {MaxAttempts}", _restartAttempts, _maxRestartAttempts);

        try
        {
            // Stop existing process
            if (_baileysProcess != null && !_baileysProcess.HasExited)
            {
                _baileysProcess.Kill(true);
                _baileysProcess.Dispose();
                _baileysProcess = null;
            }

            // Wait before restart
            await Task.Delay(5000, cancellationToken);

            // Start again
            await StartBaileysServiceAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart Baileys service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Baileys service...");

        try
        {
            if (_baileysProcess != null && !_baileysProcess.HasExited)
            {
                _logger.LogInformation("Terminating Baileys service process (PID: {ProcessId})...", _baileysProcess.Id);

                // Try graceful shutdown first
                _baileysProcess.Kill(false);

                // Wait up to 10 seconds for graceful shutdown
                if (!_baileysProcess.WaitForExit(10000))
                {
                    _logger.LogWarning("Baileys service did not exit gracefully. Forcing termination...");
                    _baileysProcess.Kill(true);
                }

                _baileysProcess.Dispose();
                _baileysProcess = null;

                _logger.LogInformation("Baileys service stopped successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Baileys service");
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _baileysProcess?.Dispose();
        _httpClient?.Dispose();
        base.Dispose();
    }
}
