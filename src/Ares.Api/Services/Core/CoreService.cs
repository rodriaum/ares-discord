using Ares.Common.Constants;
using Ares.Common.Database.Postgres;
using Ares.Common.Database.Redis;
using Ares.Common.Monitor;
using Ares.Common.Repository;

namespace Ares.Api.Services.Core;

/// <summary>
/// Service responsible for managing app core initialization and components.
/// </summary>
public class CoreService
{
    private readonly PostgresDatabase _postgresDatabase;
    private readonly RedisDatabase _redisDatabase;

    private readonly ChatModelRepository _chatModelRepository;
    private readonly GuildRepository _guildRepository;
    private readonly UserRepository _userRepository;

    private readonly ILogger<CoreService> _logger;

    public CoreService(
        PostgresDatabase postgresDatabase,
        RedisDatabase redisDatabase,
        ChatModelRepository chatModelRepository,
        GuildRepository guildRepository,
        UserRepository userRepository,
        ILogger<CoreService> logger)
    {
        _postgresDatabase = postgresDatabase;
        _redisDatabase = redisDatabase;

        _chatModelRepository = chatModelRepository;
        _guildRepository = guildRepository;
        _userRepository = userRepository;

        _logger = logger;
    }

    public async Task<bool> InitAsync()
    {
        try
        {
            await _postgresDatabase.ConnectAsync();
            await _redisDatabase.ConnectAsync();

            await _chatModelRepository.CreateTableAndIndexesAsync();
            await _guildRepository.CreateTableAndIndexesAsync();
            await _userRepository.CreateTableAndIndexesAsync();

            if (AppConstants.AppMonitorDebugMode)
            {
                var monitor = new SystemMonitor();
                _ = monitor.Init();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize core service");
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
