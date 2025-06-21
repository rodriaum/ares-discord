using Ares.Common.Constants;
using Ares.Common.Database.Postgres;
using Ares.Common.Database.Redis;
using Ares.Common.Monitor;

namespace Ares.Api.Services.Core;

/// <summary>
/// Service responsible for managing app core initialization and components.
/// </summary>
public class CoreService
{
    private readonly PostgresDatabase _postgresDatabase;
    private readonly RedisDatabase _redisDatabase;

    private readonly ILogger<CoreService> _logger;

    public CoreService(
        PostgresDatabase postgresDatabase,
        RedisDatabase redisDatabase,
        ILogger<CoreService> logger)
    {
        _postgresDatabase = postgresDatabase;
        _redisDatabase = redisDatabase;

        _logger = logger;
    }

    public async Task<bool> InitAsync()
    {
        try
        {
            await _postgresDatabase.ConnectAsync();
            await _redisDatabase.ConnectAsync();

            if (AppConstants.AppMonitorDebugMode)
            {
                var monitor = new SystemMonitor();
                _ = monitor.Init();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao inicializar o CoreService");
            return false;
        }
    }

    public async Task CloseAsync()
    {
        await _postgresDatabase.CloseAsync();
        await _redisDatabase.CloseAsync();
    }

    public bool IsDeveloper(ulong userId)
    {
        return AppConstants.DeveloperUserIds.Any(id => id.Equals(userId.ToString()));
    }
}
