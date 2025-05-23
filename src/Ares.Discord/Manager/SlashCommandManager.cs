using Ares.Core;
using Ares.Core.Constants;
using Ares.Core.Manager.Data;
using Ares.Core.Models.Data;
using Ares.Core.Models.Data.Chat.Model;
using Ares.Core.Objects;
using Ares.Core.Repository;
using Ares.Core.Util;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Text.Json;

namespace Ares.Discord.Manager;

/// <summary>
/// Handles registration, updating, and removal of slash commands for Discord guilds.
/// </summary>
public class SlashCommandManager
{
    private readonly DiscordSocketClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandManager"/> class.
    /// </summary>
    /// <param name="client">The Discord socket client.</param>
    public SlashCommandManager(DiscordSocketClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Registers all slash commands for all guilds the bot is a member of.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RegisterCommandsForAllGuildsAsync()
    {
        if (_client == null) return;

        foreach (SocketGuild guild in _client.Guilds)
        {
            await RegisterCommandsForGuildAsync(guild.Id);
        }
    }

    /// <summary>
    /// Registers all slash commands for a specific guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to register commands for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RegisterCommandsForGuildAsync(ulong guildId)
    {
        if (_client == null) return;

        GuildRepository? repository = AppCore.GuildRepository;
        if (repository == null) return;

        Guild? guild = await repository.FetchAsync(guildId);
        if (guild == null) return;

        SocketGuild? socketGuild = _client.GetGuild(guildId);
        if (socketGuild == null) return;

        List<SlashCommandBuilder> commands = BuildCommandsForGuild(guild);

        try
        {
            // Register each command with Discord
            foreach (SlashCommandBuilder command in commands)
            {
                SlashCommandProperties build = command.Build();
                await socketGuild.CreateApplicationCommandAsync(build);
                await AresLogger.LogAsync("Commands", $"Command \"{build.Name}\" registered for guild \"{guildId}\"");
            }
        }
        catch (HttpException e)
        {
            await LogCommandRegistrationError(guild.Id, e);
        }
    }

    /// <summary>
    /// Removes all existing slash commands for a specific guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to remove commands from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveCommandsForGuildAsync(ulong guildId)
    {
        if (_client == null) return;

        SocketGuild? socketGuild = _client.GetGuild(guildId);
        if (socketGuild == null) return;

        try
        {
            var existingCommands = await socketGuild.GetApplicationCommandsAsync();

            // Delete each command
            foreach (var command in existingCommands)
            {
                await command.DeleteAsync();
                await AresLogger.LogAsync("Commands", $"Command \"{command.Name}\" removed from guild \"{guildId}\"");
            }
        }
        catch (HttpException e)
        {
            await LogCommandRegistrationError(guildId, e);
        }
    }

    /// <summary>
    /// Updates slash commands for a guild after a language change.
    /// </summary>
    /// <param name="guildId">The ID of the guild to update commands for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateCommandsAfterLanguageChangeAsync(ulong guildId)
    {
        // First remove existing commands
        await RemoveCommandsForGuildAsync(guildId);

        // Then register new commands with updated language
        await RegisterCommandsForGuildAsync(guildId);

        await AresLogger.LogAsync("Commands", $"Commands updated for guild {guildId} after language change.");
    }

    /// <summary>
    /// Builds a list of slash commands for a specific guild.
    /// </summary>
    /// <param name="guild">The guild to build commands for.</param>
    /// <returns>A list of slash command builders.</returns>
    private List<SlashCommandBuilder> BuildCommandsForGuild(Guild guild)
    {
        List<ApplicationCommandOptionChoiceProperties> langOptionChoices = AppCore.LangManager.GetLanguages()
            .Select(category => new ApplicationCommandOptionChoiceProperties
            {
                Name = category.Name,
                Value = category.Code
            })
            .ToList();

        SlashCommandOptionBuilder[] configTokenOptions = Enum.GetValues(typeof(ModelCategory))
            .Cast<ModelCategory>()
            // Add all models.
            .Select(model => new SlashCommandOptionBuilder
            {
                Type = ApplicationCommandOptionType.String,
                Name = model.ToString().ToLower(),
                Description = (model.GetEndpoint() != null ? $"Access: {model.GetEndpoint()}" : "Access the Panel"),
                IsRequired = false
            })
            // Add imgur token to permanent media.
            .Concat(
            [
                    new SlashCommandOptionBuilder
                    {
                        Type = ApplicationCommandOptionType.String,
                        Name = "imgur",
                        Description = "Access: https://api.imgur.com/oauth2/addclient",
                        IsRequired = false
                    }
            ])
            .ToArray();

        return new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder()
                    .WithName("ping")
                    .WithDescription(GuildDataManager.GetTranslation(guild, LanguageKeys.PingDescription)),

                new SlashCommandBuilder()
                    .WithName("config-token")
                    .WithDescription(GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigTokenDescription))
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOptions(configTokenOptions),

                new SlashCommandBuilder()
                    .WithName("config-id")
                    .WithDescription(GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdDescription))
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOptions(
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.Role,
                            Name = "role-member",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdRoleMember),
                            IsRequired = false
                        },
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.Role,
                            Name = "role-usage",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdRoleUsage),
                            IsRequired = false
                        },
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.Role,
                            Name = "role-exclusive",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdRoleExclusive),
                            IsRequired = false
                        },
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.Channel,
                            Name = "channel-setup",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdChannelSetup),
                            IsRequired = false
                        },
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.Channel,
                            Name = "channel-log",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdChannelLog),
                            IsRequired = false
                        },
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.Channel,
                            Name = "category-chats",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigIdCategoryChats),
                            IsRequired = false
                        }
                    ),

                new SlashCommandBuilder()
                    .WithName("config-lang")
                    .WithDescription(GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigLangDescription))
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOptions(
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.String,
                            Name = "lang",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.ConfigLangOption),
                            IsRequired = true,
                            Choices = langOptionChoices
                        }
                    ),

                new SlashCommandBuilder()
                    .WithName("setup")
                    .WithDescription(GuildDataManager.GetTranslation(guild, LanguageKeys.SetupDescription))
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOptions(
                        new SlashCommandOptionBuilder
                        {
                            Type = ApplicationCommandOptionType.String,
                            Name = "option",
                            Description = GuildDataManager.GetTranslation(guild, LanguageKeys.SetupOption),
                            IsRequired = true,
                            Choices = new List<ApplicationCommandOptionChoiceProperties>
                            {
                                new ApplicationCommandOptionChoiceProperties
                                {
                                    Name = GuildDataManager.GetTranslation(guild, LanguageKeys.SetupOptionAll),
                                    Value = "setup-ai-menu"
                                },
                                new ApplicationCommandOptionChoiceProperties
                                {
                                    Name = GuildDataManager.GetTranslation(guild, LanguageKeys.SetupOptionWeb),
                                    Value = "setup-ai-web-menu"
                                },
                                new ApplicationCommandOptionChoiceProperties
                                {
                                    Name = GuildDataManager.GetTranslation(guild, LanguageKeys.SetupOptionLocal),
                                    Value = "setup-ai-local-menu"
                                }
                            }
                        }
                    )
            };
    }

    /// <summary>
    /// Registers a custom slash command for a specific guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to register the command for.</param>
    /// <param name="commandBuilder">The command builder defining the custom command.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RegisterCustomCommandForGuildAsync(ulong guildId, SlashCommandBuilder commandBuilder)
    {
        if (_client == null) return;

        SocketGuild? socketGuild = _client.GetGuild(guildId);
        if (socketGuild == null) return;

        try
        {
            SlashCommandProperties build = commandBuilder.Build();
            await socketGuild.CreateApplicationCommandAsync(build);
            await AresLogger.LogAsync("Commands", $"Custom command \"{build.Name}\" registered for guild {guildId}.");
        }
        catch (HttpException e)
        {
            await LogCommandRegistrationError(guildId, e);
        }
    }

    /// <summary>
    /// Logs an error that occurred during command registration.
    /// </summary>
    /// <param name="guildId">The ID of the guild where the error occurred.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LogCommandRegistrationError(ulong guildId, HttpException exception)
    {
        string json = await JsonUtil.ObjectToStringAsync<IReadOnlyCollection<DiscordJsonError>>(
            exception.Errors,
            serializerOptions: new JsonSerializerOptions { WriteIndented = true }
        );

        await AresLogger.LogAsync(
            "Commands",
            $"Unable to process commands for guild \"{guildId}\"",
            extra: (!(string.IsNullOrEmpty(json) || json.Equals("[]")) ? json : exception.Message),
            severity: Severity.Error
        );
    }
}