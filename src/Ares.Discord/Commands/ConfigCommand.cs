/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Models;
using Ares.Core.Models.Config;
using Ares.Core.Models.Token;
using Ares.Core.Objects.Language;
using Ares.Core.Repository;
using Ares.Core.Service;
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

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (_client == null || !(command.Data.Name.Equals("config-token") || command.Data.Name.Equals("config-id") || command.Data.Name.Equals("config-lang"))) return;

        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Config")
            .WithDescription("Aguarde...")
            .WithColor(Color.Gold)
            .WithFooter($"{DateTime.Now.Year} - {AresConstant.AppName}");

        await command.RespondAsync(ephemeral: true, embed: embed.Build());
        RestInteractionMessage message = await command.GetOriginalResponseAsync();

        GuildRepository? repository = AresCore.GuildRepository;

        if (repository == null)
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

        Guild? guild = await repository.FetchAsync(guildId.Value);
        const int maxAttempts = 3;

        for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription($"Servidor não foi encontrado no banco de dados! A criar... ({attempts}/{maxAttempts})")
                    .WithColor(Color.Red)
                    .Build());

            await Task.Delay(1500);
            guild = await repository.SaveAsync(guildId.Value);
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

        GTokenModel tokenData = guild.Token;
        GuildConfigData configData = guild.Config;

        StringBuilder sb = new StringBuilder();

        bool tokenChange = false, configChange = false;

        foreach (var option in options)
        {
            if (option == null)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription(GuildService.GetTranslation(guild, LangKeys.InvalidOptions))
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            string optionName = option.Name;
            string? optionValue = option.Value.ToString();

            if (optionValue == null)
            {
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription(GuildService.GetTranslation(guild, LangKeys.InvalidOptionValue))
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            switch (optionName)
            {

                /*
                 * Token Configuration
                 * It only saves if the token is different. There is no message to prevent brute force.
                 */

                case "openai":
                    tokenData.OpenAi = optionValue;
                    tokenChange = tokenData.OpenAi != optionValue;
                    break;

                case "anthropic":
                    tokenData.Anthropic = optionValue;
                    tokenChange = tokenData.Anthropic != optionValue;
                    break;

                case "deepseek":
                    tokenData.Deepseek = optionValue;
                    tokenChange = tokenData.Deepseek != optionValue;
                    break;

                case "xai":
                    tokenData.xAI = optionValue;
                    tokenChange = tokenData.xAI != optionValue;
                    break;

                case "google":
                    tokenData.Google = optionValue;
                    tokenChange = tokenData.Google != optionValue;
                    break;

                case "imgur":
                    tokenData.Imgur = optionValue;
                    tokenChange = tokenData.Imgur != optionValue;
                    break;

                /*
                 * IDs Configuration
                 */

                case "role-member":
                    configData.MemberRoleId = ulong.Parse(optionValue);
                    configChange = true;
                    break;

                case "role-usage":
                    configData.UsageRoleId = ulong.Parse(optionValue);
                    configChange = true;
                    break;

                case "role-exclusive":
                    configData.ExclusiveRoleId = ulong.Parse(optionValue);
                    configChange = true;
                    break;

                case "channel-setup":
                    configData.SetupChannelId = ulong.Parse(optionValue);
                    configChange = true;
                    break;

                case "channel-log":
                    configData.LogChannelId = ulong.Parse(optionValue);
                    configChange = true;
                    break;

                case "category-chats":
                    configData.ChatsCategoryId = ulong.Parse(optionValue);
                    configChange = true;
                    break;

                /*
                 * Lang Configuration
                 */

                case "lang":
                    configData.Lang = optionValue;
                    configChange = true;
                    break;

                /*
                 * Default Option
                 */

                default:
                    await message.ModifyAsync(msg =>
                        msg.Embed = embed
                            .WithDescription(GuildService.GetTranslation(guild, LangKeys.InvalidOption))
                            .WithColor(Color.Red)
                            .Build()
                    );
                    return;
            }

            sb.AppendLine(GuildService
                .GetTranslation(guild, LangKeys.ConfigUpdateSuccess)
                .Replace("{0}", optionName ?? "N/A")
                .Replace("{1}", optionValue ?? "N/A"));
        }

        // Only one option can be changed per command, hence the use of only one boolean variable.
        bool success = false;

        if (tokenChange)
        {
            success = await GuildService.SaveTokenDataAsync(guild, tokenData);
        }

        if (configChange)
        {
            success = await GuildService.SaveConfigDataAsync(guild, configData);
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
                    .WithDescription(GuildService.GetTranslation(guild, LangKeys.ConfigUpdateUnSuccess))
                    .WithColor(Color.Gold)
                    .Build()
                );
        }
    }
}