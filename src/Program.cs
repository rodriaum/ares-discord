using Ares.src.Commands;
using Ares.src.Database;
using Ares.src.Database.Collection;
using Ares.src.Database.Mongo;
using Ares.src.Listener;
using Ares.src.Listener.Chat;
using Ares.src.Manager;
using Ares.src.Service;
using Ares.src.Util;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using DotNetEnv;
using Newtonsoft.Json;

namespace Ares.src;

/// <summary>
/// Main application class that initializes and manages the Discord bot.
/// </summary>
internal class Program
{
    /// <summary>
    /// Gets or sets the MongoDB database instance.
    /// </summary>
    public static MongoDatabase? Database { get; private set; }

    /// <summary>
    /// Gets or sets the guild collection for database operations.
    /// </summary>
    public static GuildCollection? GuildCollection { get; private set; }

    /// <summary>
    /// Guild manager instance for handling guild-related operations.
    /// </summary>
    public static GuildManager GuildManager = new GuildManager();

    /// <summary>
    /// Language manager instance for handling localization.
    /// </summary>
    public static LangManager LangManager = new LangManager();

    /// <summary>
    /// Discord client instance for bot operations.
    /// </summary>
    private static DiscordSocketClient? _client { get; set; }

    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    static async Task Main()
    {
        // Load environment variables
        Env.Load();

        // Initialize database connection
        InitDatabase();

        // Configure Discord client with appropriate intents
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMembers |
                             GatewayIntents.GuildBans |
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

        // Connect to Discord
        await _client.LoginAsync(TokenType.Bot, Env.GetString("DISCORD_TOKEN"));
        await _client.StartAsync();

        new LoggingService(client: _client);

        // Initialize event listeners
        InitListeners();

        // Register commands
        await RegisterCommands();

        // Initialize managers
        new AiManager();

        // Configure bot status
        await ConfigureBotStatus();

        // Subscribe to events
        _client.Ready += RegisterCommands;
        _client.Ready += () =>
        {
            LogUtil.Log("Status", $"Success! Logged \"{_client.CurrentUser.Username}\"");
            return Task.CompletedTask;
        };

        // Keep the application running
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// Initializes the database connection.
    /// </summary>
    private static void InitDatabase()
    {
        MongoDatabase database = new MongoDatabase(new DatabaseCredentials
        {
            Host = "127.0.0.1",
            Database = "ares",
            Port = 27017
        });

        database.Connect();

        Database = database;
        GuildCollection = new GuildCollection(database);
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
        var langOptionChoices = Program.LangManager.GetLanguages()
            .Select(langCategory => new ApplicationCommandOptionChoiceProperties
            {
                Name = langCategory.Name,
                Value = langCategory.Code
            })
            .ToList();

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
                .AddOptions(
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "openai",
                        Description = "Access: https://platform.openai.com/settings/organization/api-keys",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "anthropic",
                        Description = "Access: https://console.anthropic.com/settings/keys",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "deepseek",
                        Description = "Access: https://platform.deepseek.com/api_keys",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "imgur",
                        Description = "Access: https://api.imgur.com/oauth2/addclient",
                        IsRequired = false
                    }
                ),

            // ID configuration command
            new SlashCommandBuilder()
                .WithName("config-id")
                .WithDescription("Configure channels for the current server.")
                .AddOptions(
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "role-member",
                        Description = "Enter the member role ID.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "role-usage",
                        Description = "Enter the ID of the role that can use chats.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "role-exclusive",
                        Description = "Enter the ID of the role that can use exclusive chats.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "channel-setup",
                        Description = "Enter the ID of the channel where the chat embed will be.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "channel-log",
                        Description = "Enter the ID of the channel where bot logs will be kept.",
                        IsRequired = false
                    },
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "category-chats",
                        Description = "Enter the ID of the category where generated chat channels will be kept.",
                        IsRequired = false
                    }
                ),

            // Language configuration command
            new SlashCommandBuilder()
                .WithName("config-lang")
                .WithDescription("Choose the application language for this server.")
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
                .AddOptions(
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "option",
                        Description = "Choose the type of setup to perform in the current channel.",
                        IsRequired = true,
                        Choices = new List<ApplicationCommandOptionChoiceProperties>
                        {
                            new ApplicationCommandOptionChoiceProperties { Name = "AI Generation", Value = "setup-ai-menu" }
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
                var build = command.Build();
                LogUtil.Log("Commands", $"Command \"{build.Name}\" registered.");
            }
        }
        catch (HttpException e)
        {
            // Handle and log any errors during command registration
            string json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
            LogUtil.Log("Commands", "Unable to register commands.\n -> " +
                        (!(string.IsNullOrEmpty(json) || json.Equals("[]")) ? json : e.Message));
        }
    }
}