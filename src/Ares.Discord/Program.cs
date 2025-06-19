/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Manager;
using Ares.Core.Objects;
using Ares.Core.Util;
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
    /// Base URL for API requests.
    /// </summary>
    public static string? ApiBaseUrl { get; private set; }

    #endregion

    #region Main Entry Point

    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    static async Task Main()
    {
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
        await LangManager.Init();

        // Configure and start Discord client
        await ConfigureDiscordClient();

        // Setup event handlers
        SetupEventHandlers();

        // Keep the application running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Expected when token is canceled
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