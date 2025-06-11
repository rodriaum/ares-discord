/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Constants;
using Ares.Core.Objects;
using Ares.Core.Util;
using Ares.Discord.Commands;
using Ares.Discord.Listener;
using Ares.Discord.Listener.Chat;
using Ares.Discord.Manager;
using Ares.Discord.Service;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DotNetEnv;
using System.Diagnostics;

namespace Ares.Discord;

/// <summary>
/// Main application class that initializes and manages the Discord bot.
/// </summary>
public class Program
{
    /// <summary>
    /// Slash command manager.
    /// </summary>
    public static SlashCommandManager? _commandManager { get; private set; }

    /// <summary>
    /// Discord client instance for bot operations.
    /// </summary>
    private static DiscordSocketClient? _client { get; set; }

    /// <summary>
    /// Discord command service instance.
    /// </summary>
    private static CommandService? _commands { get; set; }

    /// <summary>
    /// Cancellation token source for graceful shutdown of the application.
    /// </summary>
    private static readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string currentProcessName = Process.GetCurrentProcess().ProcessName;
        bool isRunning = Process.GetProcessesByName(currentProcessName).Length > 1;

        if (isRunning)
        {
            await AresLogger.LogAsync("Status", "Application is already running.", severity: Severity.Error);
            return;
        }

        Env.Load(); // Load environment variables in build/run path
        Env.Load($@"{AppConstants.ProjectPath}.env"); // Load environment variables in project path

        if (!await AppCore.Init())
        {
            AresLogger.Log("Database", "Core initialization failed.", severity: Severity.Critical);
            return;
        }

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

        // Initialize event listeners
        InitListeners();

        // Register command manager
        _commandManager = new SlashCommandManager(_client);

        // Configure bot status
        await ConfigureBotStatus();

        _client.Ready += async () =>
        {
            await _commandManager.RegisterCommandsForAllGuildsAsync();
            AresLogger.Log("Status", $"Yup! Logged with \"{_client.CurrentUser.Username}\"", severity: Severity.Success);
        };

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            await AresLogger.LogAsync("Exit", "Shutting down core system...");
            await AppCore.Close();
            await AresLogger.LogAsync("Exit", "Core system shutdown.");
            _cts.Cancel();
            Environment.Exit(0);
        };

        // Keep the application running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, _cts.Token);
        }
        // Just ignore the exception as it is expected behavior when canceling the token.
        catch (TaskCanceledException) { }
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
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task ConfigureBotStatus()
    {
        if (_client == null) return;

        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        await _client.SetGameAsync("https://github.com/rodriaum", type: ActivityType.CustomStatus);
    }
}