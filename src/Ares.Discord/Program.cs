/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.DTOs;
using Ares.Common.Manager;
using Ares.Common.Objects;
using Ares.Common.Util;
using Ares.Discord.Commands;
using Ares.Discord.Listener;
using Ares.Discord.Listener.Chat;
using Ares.Discord.Manager;
using Ares.Discord.Service;
using Ares.Discord.Services.Api;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.Extensions.AI;
using System.Diagnostics;

namespace Ares.Discord;

/// <summary>
/// Main application class that initializes and manages the Discord bot.
/// </summary>
public class Program
{
    #region Private Fields

    /// <summary>
    /// Discord client instance for bot operations.
    /// </summary>
    private static DiscordSocketClient? _client;

    /// <summary>
    /// Discord command service instance.
    /// </summary>
    private static CommandService? _commands;

    /// <summary>
    /// Cancellation token source for graceful shutdown of the application.
    /// </summary>
    private static readonly CancellationTokenSource _cts = new();

    #endregion

    #region Public Properties

    /// <summary>
    /// Slash command manager.
    /// </summary>
    public static SlashCommandManager? _commandManager { get; private set; }

    /// <summary>
    /// Ollama client instance for AI chat operations.
    /// </summary>
    public static IChatClient? OllamaClient { get; private set; }

    /// <summary>
    /// Language manager instance for handling localization.
    /// </summary>
    public static LanguageManager LangManager = new LanguageManager();

    /// <summary>
    /// HTTP client instance for making API requests.
    /// </summary>
    public static HttpClient? HttpClient { get; private set; }

    /// <summary>
    /// Service for interacting with user data.
    /// </summary>
    public static UserService? UserService { get; private set; }

    /// <summary>
    /// Service for interacting with guild data.
    /// </summary>
    public static GuildService? GuildService { get; private set; }

    /// <summary>
    /// Service for interacting with chat models.
    /// </summary>
    public static ChatModelService? ChatModelService { get; private set; }

    /// <summary>
    /// Service for interacting with system data.
    /// </summary>
    public static SystemService? SystemService { get; private set; }

    /// <summary>
    /// Base URL for API requests.
    /// </summary>
    public static string? ApiBaseUrl { get; private set; }
    
    /// <summary>
    /// Indicates whether the application has been initialized.
    /// </summary>
    public static bool IsStarting { get; private set; } = true;
    
    /// <summary>
    /// Indicates whether the application is in the process of shutting down.
    /// </summary>
    public static bool IsShuttingDown { get; private set; } = false;

    #endregion

    #region Main Entry Point

    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    static async Task Main()
    {
        IsStarting = true;
        
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Check if application is already running
        string currentProcessName = Process.GetCurrentProcess().ProcessName;
        bool isRunning = Process.GetProcessesByName(currentProcessName).Length > 1;

        if (isRunning)
        {
            await AresLogger.LogAsync("Status", "Application is already running.", severity: Severity.Error);
            return;
        }

        // Load environment configuration
        Env.Load();
        Env.Load($@"{AppConstants.ProjectPath}.env");

        ApiBaseUrl = Env.GetString("API_BASE_URL");

        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            await AresLogger.LogAsync("Status", "API base URL is not configured.", severity: Severity.Error);
            return;
        }

        // Initialize services
        InitializeServices();

        if (SystemService == null)
        {
            await AresLogger.LogAsync("Status", "System service is not initialized.", severity: Severity.Error);
            return;
        }

        ApiResult<string>? systemInfoResult = await SystemService.GetSystemStatus();

        int count = 0;
        int limit = 3;
        int seconds = 15;

        while (systemInfoResult == null || !systemInfoResult.Success)
        {
            if (count >= limit)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                await AresLogger.LogAsync(null, $"Failed to connect to the API. Exiting... ({count + 1}/{limit})", severity: Severity.Error);
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            await AresLogger.LogAsync(null, "System information is not available. Please check the API connection.", severity: Severity.Error);
            await AresLogger.LogAsync(null, $"Retrying to connect to the API... ({count + 1}/{limit})", severity: Severity.Info);
            Console.Write("\n");

            for (int i = 1; i <= seconds; i++)
            {
                ConsoleColor cor = i < 5 ? ConsoleColor.Red : i < 10 ? ConsoleColor.Yellow : ConsoleColor.Green;
                ProgressBarUtil.DrawProgressBar(i, seconds, cor, useColor: true);
                await Task.Delay(1000);
            }

            count++;
            systemInfoResult = await SystemService.GetSystemStatus();
        }

        Console.Clear();
        AresLogger.Log("Status", $"API System information: {systemInfoResult.Message}", severity: Severity.Info);

        await LangManager.Init();
        await ConfigureDiscordClient();
        SetupEventHandlers();

        IsStarting = false;

        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (TaskCanceledException)
        {
            IsShuttingDown = true;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes all services used by the application.
    /// </summary>
    private static void InitializeServices()
    {
        HttpClient = new HttpClient();
        UserService = new UserService(HttpClient, ApiBaseUrl!);
        GuildService = new GuildService(HttpClient, ApiBaseUrl!);
        ChatModelService = new ChatModelService(HttpClient, ApiBaseUrl!);
        SystemService = new SystemService(HttpClient, ApiBaseUrl!);

        Uri ollamaUri = new Uri($"http://{Env.GetString("OLLAMA_HOST")}:{Env.GetInt("OLLAMA_PORT")}");
        OllamaClient = new OllamaChatClient(ollamaUri);
    }

    /// <summary>
    /// Configures and starts the Discord client.
    /// </summary>
    private static async Task ConfigureDiscordClient()
    {
        DiscordSocketConfig config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds |
                            GatewayIntents.GuildMembers |
                            GatewayIntents.GuildEmojis |
                            GatewayIntents.GuildIntegrations |
                            GatewayIntents.GuildWebhooks |
                            GatewayIntents.GuildVoiceStates |
                            GatewayIntents.GuildMessages |
                            GatewayIntents.GuildMessageReactions |
                            GatewayIntents.GuildMessageTyping |
                            GatewayIntents.DirectMessages |
                            GatewayIntents.DirectMessageReactions |
                            GatewayIntents.DirectMessageTyping |
                            GatewayIntents.MessageContent |
                            GatewayIntents.AutoModerationConfiguration |
                            GatewayIntents.AutoModerationActionExecution |
                            GatewayIntents.GuildMessagePolls |
                            GatewayIntents.DirectMessagePolls
        };

        _client = new DiscordSocketClient(config);
        _commands = new CommandService();

        string discordToken = (AppConstants.AppDevMode ? Env.GetString("DISCORD_TOKEN_DEV") : Env.GetString("DISCORD_TOKEN"));
        if (string.IsNullOrWhiteSpace(discordToken))
        {
            await AresLogger.LogAsync("Token", $"Could not find application token. (AppDevMode={AppConstants.AppDevMode})", severity: Severity.Error);
            return;
        }

        await _client.LoginAsync(TokenType.Bot, discordToken);
        await _client.StartAsync();

        new LoggingService(client: _client, command: _commands);
    }

    /// <summary>
    /// Sets up all event handlers and initializes listeners.
    /// </summary>
    private static void SetupEventHandlers()
    {
        if (_client == null) return;

        // Initialize event listeners
        InitListeners();

        // Register command manager
        _commandManager = new SlashCommandManager(_client);

        // Configure bot status
        _ = ConfigureBotStatus();

        _client.Ready += async () =>
        {
            await _commandManager.RegisterCommandsForAllGuildsAsync();
            AresLogger.Log("Status", $"Yup! Logged with \"{_client.CurrentUser.Username}\"", severity: Severity.Success);
        };

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            await AresLogger.LogAsync("Exit", "Shutting down core system...");
            await AresLogger.LogAsync("Exit", "Core system shutdown.");
            _cts.Cancel();
            Environment.Exit(0);
        };
    }

    /// <summary>
    /// Initializes all event listeners for the Discord client.
    /// </summary>
    private static void InitListeners()
    {
        if (_client == null) return;

        // Chat and interaction listeners
        new SelectedChatListener(_client);
        new ChatButtonListener(_client);
        new ReceivedContentListener(_client);
        new GuildListener(_client);
        new ChatImageOptionListener(_client);
        new ChatCodeSnippetListener(_client);

        // Command handlers
        new PingCommand(_client);
        new SetupCommand(_client);
        new ConfigCommand(_client);
    }

    /// <summary>
    /// Configures the bot's online status and activity.
    /// </summary>
    private static async Task ConfigureBotStatus()
    {
        if (_client == null) return;

        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        await _client.SetGameAsync("https://github.com/rodriaum", type: ActivityType.CustomStatus);
    }

    #endregion

    #region Public Methods

    public static bool IsDeveloper(object id)
    {
        return AppConstants.DeveloperUserIds.Any(dev => dev == id.ToString());
    }

    #endregion
}