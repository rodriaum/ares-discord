namespace Ares.Api.Services.Core;

public class CoreHostedService : IHostedService
{
    private readonly CoreService _coreService;
    private readonly ILogger<CoreHostedService> _logger;

    public CoreHostedService(CoreService coreService, ILogger<CoreHostedService> logger)
    {
        _coreService = coreService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting core service...");
        bool success = await _coreService.InitAsync();

        if (!success)
        {
            _logger.LogError("Core service failed to initialize.");
        }
        else
        {
            _logger.LogInformation("Cores ervice initialized successfully.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down core service...");
        await _coreService.CloseAsync();
        _logger.LogInformation("Core service has been shutdown.");
    }
}