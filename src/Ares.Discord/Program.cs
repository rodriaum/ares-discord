/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Constants;
using Ares.Core.Models.Data.Chat.Model;
using Ares.Core.Objects;
using Ares.Core.Util;
using Ares.Discord.Commands;
using Ares.Discord.Listener;
using Ares.Discord.Listener.Chat;
using Ares.Discord.Service;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using DotNetEnv;
using System.Diagnostics;
using System.Text.Json;

namespace Ares.Discord;

/// <summary>
/// Main application class that initializes and manages the Discord bot.
/// </summary>
public class Program
{
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

        // Register commands
        await RegisterCommands();

        // Configure bot status
        await ConfigureBotStatus();

        _client.Ready += RegisterCommands;
        _client.Ready += () =>
        {
            AresLogger.Log("Status", $"Yup! Logged with \"{_client.CurrentUser.Username}\"", severity: Severity.Success);
            return Task.CompletedTask;
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

    /// <summary>
    /// Registers all slash commands with Discord API.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RegisterCommands()
    {
        // Get available language options
        var langOptionChoices = AppCore.LangManager.GetLanguages()
            .Select(category => new ApplicationCommandOptionChoiceProperties
            {
                Name = category.Name,
                Value = category.Code
            })
            .ToList();

        var configTokenOptions = Enum.GetValues(typeof(ModelCategory))
            .Cast<ModelCategory>()
            // Add all models.
            .Select(model => new SlashCommandOptionBuilder
            {
                Type = ApplicationCommandOptionType.String,
                Name = model.ToString().ToLower(), // Fix: Name cannot contain any uppercase characters.
                Description = (model.GetEndpoint() != null ? $"Access: {model.GetEndpoint()}" : "Access the Panel"),
                IsRequired = false
            })
            // Add imgur token to permanent media.
            .Concat(new[]
            {
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.String,
                    Name = "imgur",
                    Description = "Access: https://api.imgur.com/oauth2/addclient",
                    IsRequired = false
                }
            })
            .ToArray();


        // Define all slash commands
        List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>
        {
            // Ping command for latency check
            new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Current gateway ping"),

            // Token configuration command
            new SlashCommandBuilder()
                .WithName("config-token")
                .WithDescription("Configure tokens for the current server.")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOptions(configTokenOptions),

            // ID configuration command
            new SlashCommandBuilder()
                .WithName("config-id")
                .WithDescription("Configure channels for the current server.")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOptions(
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.Role,
                        Name = "role-member",
                        Description = "Enter the member role ID.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.Role,
                        Name = "role-usage",
                        Description = "Enter the ID of the role that can use chats.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.Role,
                        Name = "role-exclusive",
                        Description = "Enter the ID of the role that can use exclusive chats.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.Channel,
                        Name = "channel-setup",
                        Description = "Enter the ID of the channel where the chat embed will be.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.Channel,
                        Name = "channel-log",
                        Description = "Enter the ID of the channel where bot logs will be kept.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.Channel,
                        Name = "category-chats",
                        Description = "Enter the ID of the category where generated chat channels will be kept.",
                        IsRequired = false
                    }
                ),

            // Language configuration command
            new SlashCommandBuilder()
                .WithName("config-lang")
                .WithDescription("Choose the application language for this server.")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOptions(
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "lang",
                        Description = "Choose the language.",
                        IsRequired = true,
                        Choices = langOptionChoices
                    }
                ),

            // Setup command
            new SlashCommandBuilder()
                .WithName("setup")
                .WithDescription("Choose the type of setup to perform in the current channel.")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOptions(
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "option",
                        Description = "Choose the type of setup to perform in the current channel.",
                        IsRequired = true,
                        Choices = new List<ApplicationCommandOptionChoiceProperties>
                        {
                            new ApplicationCommandOptionChoiceProperties { Name = "AI Generation (All)", Value = "setup-ai-menu" },
                            new ApplicationCommandOptionChoiceProperties { Name = "AI Generation (Web)", Value = "setup-ai-web-menu" },
                            new ApplicationCommandOptionChoiceProperties { Name = "AI Generation (Local)", Value = "setup-ai-local-menu" }
                        }
                    }
                )
        };

        try
        {
            // Only register commands if client is initialized
            if (_client == null)
                return;

            // Register each command with Discord
            foreach (SlashCommandBuilder command in commands)
            {
                SlashCommandProperties build = command.Build();
                await _client.CreateGlobalApplicationCommandAsync(build);
                AresLogger.Log("Commands", $"Command \"{build.Name}\" registered.");
            }
        }
        catch (HttpException e)
        {
            // Handle and log any errors during command registration
            string json = await JsonUtil.ObjectToStringAsync<IReadOnlyCollection<DiscordJsonError>>
                (
                    e.Errors,
                    serializerOptions: new JsonSerializerOptions { WriteIndented = true }
                );

            await AresLogger.LogAsync
                (
                    "Commands",
                    "Unable to register commands.",
                    extra: (!(string.IsNullOrEmpty(json) || json.Equals("[]")) ? json : e.Message),
                    severity: Severity.Error
                );
        }
    }
}