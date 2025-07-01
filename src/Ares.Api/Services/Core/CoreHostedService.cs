namespace Ares.Api.Services.Core;

public class CoreHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CoreHostedService> _logger;

    public CoreHostedService(IServiceProvider serviceProvider, ILogger<CoreHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting core service...");

        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            CoreService coreService = scope.ServiceProvider.GetRequiredService<CoreService>();
            bool success = await coreService.InitAsync();

            if (!success)
            {
                _logger.LogError("Core service failed to initialize.");
            }
            else
            {
                _logger.LogInformation("Core service initialized successfully.");
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            CoreService coreService = scope.ServiceProvider.GetRequiredService<CoreService>();
            await coreService.CloseAsync();

            _logger.LogInformation("Core service stopped.");
        }
    }
}