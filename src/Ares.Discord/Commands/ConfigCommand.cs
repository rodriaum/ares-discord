﻿/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.DTOs;
using Ares.Common.Models.Data;
using Ares.Common.Models.Preference;
using Ares.Common.Models.Token;
using Ares.Discord.Manager;
using Ares.Discord.Services.Api;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text;

namespace Ares.Discord.Commands;

public class ConfigCommand
{
    private readonly DiscordSocketClient? _client;

    public ConfigCommand(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += SlashCommandHandler;
        _client = client;
    }

    private Task SlashCommandHandler(SocketSlashCommand command)
    {
        _ = Task.Run(async () =>
        {
            if (_client == null || !(command.Data.Name.Equals("config-token") || command.Data.Name.Equals("config-id") || command.Data.Name.Equals("config-lang")))
                return;
            
            if (Program.IsStarting || Program.IsShuttingDown) return;

            EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Config")
            .WithDescription("Aguarde...")
            .WithColor(Color.Gold)
            .WithFooter($"{DateTime.Now.Year} - {AppConstants.AppName}");

            await command.RespondAsync(ephemeral: true, embed: embed.Build());
            RestInteractionMessage message = await command.GetOriginalResponseAsync();



            GuildService? guildService = Program.GuildService;

            if (guildService == null)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription("Não foi possível acessar as informações do servidor atual.")
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            ulong? guildId = command.GuildId;

            if (guildId == null)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription("Não foi possível encontrar o ID do servidor atual.")
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            ApiResult<Guild>? guildResult = await guildService.GetGuild(guildId.Value);

            if (guildResult == null || !guildResult.Success)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription("Não foi possível acessar as informações do servidor atual.")
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            Guild? guild = guildResult.Data;

            const int maxAttempts = 3;

            for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription($"Servidor não foi encontrado no banco de dados! A criar... ({attempts}/{maxAttempts})")
                        .WithColor(Color.Red)
                        .Build());

                await Task.Delay(1500);


                guildResult = await guildService.CreateOrGetGuild(guildId.Value);

                if (guildResult == null || !guildResult.Success)
                {
                    await message.ModifyAsync(msg =>
                        msg.Embed = embed
                            .WithDescription("Não foi possível acessar as informações do servidor atual.")
                            .WithColor(Color.Red)
                            .Build()
                    );
                    return;
                }

                guild = guildResult.Data;
            }

            if (guild == null)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription("Não foi possível criar esse servidor no banco de dados! Tente novamente mais tarde.")
                        .WithColor(Color.Red)
                        .Build());
                return;
            }

            IReadOnlyCollection<SocketSlashCommandDataOption> options = command.Data.Options;

            GToken tokenData = guild.Token;
            GPreference configData = guild.Preferences;

            StringBuilder sb = new StringBuilder();

            bool tokenChange = false, configChange = false;

            foreach (var option in options)
            {
                if (option == null)
                {
                    await message.ModifyAsync(msg =>
                        msg.Embed = embed
                            .WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidOptions))
                            .WithColor(Color.Red)
                            .Build()
                    );
                    return;
                }

                string optionName = option.Name;
                object optionValue = option.Value;

                if (optionValue == null)
                {
                    await message.ModifyAsync(msg =>
                        msg.Embed = embed
                            .WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidOptionValue))
                            .WithColor(Color.Red)
                            .Build()
                    );
                    return;
                }

                string optionValueString = optionValue.ToString() ?? "";

                switch (optionName)
                {

                    /*
                     * Token Configuration
                     * It only saves if the token is different. There is no message to prevent brute force.
                     */

                    case "openai":
                    case "anthropic":
                    case "deepseek":
                    case "xai":
                    case "google":
                    case "imgur":
                    case "perplexity":
                        tokenData.SetToken(optionName, optionValueString);
                        tokenChange = tokenData.GetToken(optionValueString) != optionValueString;
                        break;

                    /*
                     * IDs Configuration
                     */

                    case "role-member":
                        if (optionValue is IRole memberRole)
                        {
                            configData.MemberRoleId = memberRole.Id;
                            configChange = true;
                        }
                        else
                        {
                            configChange = false;
                        }
                        break;

                    case "role-usage":
                        if (optionValue is IRole usageRole)
                        {
                            configData.UsageRoleId = usageRole.Id;
                            configChange = true;
                        }
                        else
                        {
                            configChange = false;
                        }
                        break;

                    case "role-exclusive":
                        if (optionValue is IRole exclusiveRole)
                        {
                            configData.ExclusiveRoleId = exclusiveRole.Id;
                            configChange = true;
                        }
                        else
                        {
                            configChange = false;
                        }
                        break;

                    case "channel-setup":
                        if (optionValue is ITextChannel setupChannel)
                        {
                            configData.SetupChannelId = setupChannel.Id;
                            configChange = true;
                        }
                        else
                        {
                            configChange = false;
                        }
                        break;

                    case "channel-log":
                        if (optionValue is ITextChannel textChannel)
                        {
                            configData.LogChannelId = textChannel.Id;
                            configChange = true;
                        }
                        else
                        {
                            configChange = false;
                        }
                        break;

                    case "category-chats":
                        if (optionValue is ICategoryChannel chatsCategory)
                        {
                            configData.ChatsCategoryId = chatsCategory.Id;
                            configChange = true;
                        }
                        else
                        {
                            configChange = false;
                        }
                        break;

                    /*
                     * Lang Configuration
                     */

                    case "lang":
                        configData.Lang = optionValueString;
                        configChange = true;

                        SlashCommandManager? command = Program._commandManager;

                        if (command != null)
                        {
                            await command.UpdateCommandsAfterLanguageChangeAsync(guild.Id);
                        }

                        break;

                    /*
                     * Default Option
                     */

                    default:
                        await message.ModifyAsync(msg =>
                            msg.Embed = embed
                                .WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.InvalidOption))
                                .WithColor(Color.Red)
                                .Build()
                        );
                        return;
                }

                sb.AppendLine(Program.LangManager
                    .GetTranslation(guild, LanguageKeys.ConfigUpdateSuccess)
                    .Replace("{0}", optionName ?? "N/A")
                    .Replace("{1}", (!string.IsNullOrWhiteSpace(optionValueString) ? optionValueString : "N/A")));
            }

            // Only one option can be changed per command, hence the use of only one boolean variable.
            bool success = false;

            if (tokenChange)
            {
                ApiResult<object>? tokenResult = await guildService.SaveTokenData(guild.Id, tokenData);
                success = tokenResult != null && tokenResult.Success;
            }

            if (configChange)
            {
                ApiResult<object>? preferenceResult = await guildService.SavePreferenceData(guild.Id, configData);
                success = preferenceResult != null && preferenceResult.Success;
            }

            if (success)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription(sb.ToString())
                        .WithColor(Color.Green)
                        .Build()
                );
            }
            else
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.ConfigUpdateUnSuccess))
                        .WithColor(Color.Gold)
                        .Build()
                    );
            }
        });

        return Task.CompletedTask;
    }
}