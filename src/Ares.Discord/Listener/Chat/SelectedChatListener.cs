/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Ares.Discord.Util;
using Ares.Core;
using Ares.Core.Database.Collection;
using Ares.Core.Database.Model;
using Ares.Core.Database.Model.Chat.Sub;
using Ares.Core.Database.Model.Config;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MongoDB.Driver;

namespace Ares.Discord.Listener.Chat;

internal class SelectedChatListener
{
    private static DiscordSocketClient? _client { get; set; }

    public SelectedChatListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
        _client = client;
    }

    private async Task SelectMenuHandler(SocketMessageComponent args)
    {
        if (!args.Data.CustomId.StartsWith("chat-menu-")) return;

        await args.RespondAsync(ephemeral: true, text: AresConstant.LoadingEmote);
        RestInteractionMessage message = await args.GetOriginalResponseAsync();

        try
        {
            SocketUser user = args.User;
            ulong guildId = args.GuildId.GetValueOrDefault();

            if (_client == null || user == null)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnableGetMember);
                return;
            }

            GuildCollection? data = AresCore.GuildCollection;

            if (data == null)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                return;
            }

            Guild? guild = await data.FetchAsync(guildId);
            const int maxAttempts = 3;

            for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
            {
                await message.ModifyAsync(it => it.Content = $"Tentando criar guilda no banco de dados... {attempts}/{maxAttempts}");
                await Task.Delay(1500);
                guild = await data.SaveAsync(guildId);
            }

            if (guild == null)
            {
                await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a guilda no banco de dados.");
                return;
            }

            SocketGuild socketGuild = _client.GetGuild(guildId);

            if (socketGuild == null)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnableGetMember);
                return;
            }

            GuildConfigData? gid = guild.Config;

            if (gid == null)
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.CouldNotFindInfoID));
                return;
            }

            IRole usageRole = socketGuild.GetRole(gid.UsageRoleId);

            if (usageRole == null)
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.RoleEliminated));
                return;
            }

            SocketGuildUser member = socketGuild.GetUser(user.Id);

            if (!member.Roles.Contains(usageRole))
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.RoleMissing).Replace("{0}", usageRole.Mention));
                return;
            }

            if (guild.HasActiveUserConversation(user))
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.ActiveConversation));
                return;
            }

            ChatModel? model = ChatModel.GetByModel(args.Data.Values.First());

            if (model == null)
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.ModelNotFound));
                return;
            }

            if (model.Dev && !AresCore.IsDeveloper(user))
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.ModelDevMode));
                return;
            }

            if (!model.Available)
            {
                await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.ModelUnavailable));
                return;
            }

            if (model.Exclusive)
            {
                IRole exclusiveRole = socketGuild.GetRole(gid.ExclusiveRoleId);

                if (exclusiveRole == null)
                {
                    await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.RoleEliminated));
                    return;
                }

                if (!member.Roles.Contains(exclusiveRole))
                {
                    await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.RoleMissing).Replace("{0}", exclusiveRole.Mention));
                    return;
                }
            }

            string emojiUnicode = AresUtil.GetEmojiByModelType(model.Type).ToString();

            SocketCategoryChannel category = socketGuild.GetCategoryChannel(gid.ChatsCategoryId);
            RestTextChannel channel = await socketGuild.CreateTextChannelAsync($"{emojiUnicode}┃{user.GlobalName}", properties => properties.CategoryId = category.Id);

            GChatInfoModel info = new GChatInfoModel
                (
                    active: true,
                    channel: channel.Id,
                    model: model.Model
                );

            DateTime time = DateTime.Now;

            string greetingKey = (time.Hour >= 5 && time.Hour < 12) ? LangKeys.GoodMorning :
                  (time.Hour >= 12 && time.Hour < 18) ? LangKeys.GoodAfternoon :
                  LangKeys.GoodNight;

            string helloMessage = string.Format(guild.GetTranslation(LangKeys.HelloMessage), guild.GetTranslation(greetingKey), user.GlobalName);

            GChatHistoricModel historic = new GChatHistoricModel(system: helloMessage);
            info.Historics.Add(historic);

            if (!await guild.CreateChatData(user, info))
            {
                await channel.DeleteAsync();
                await Task.Delay(500);
                return;
            }

            EmbedBuilder infoEmbed = new EmbedBuilder()
                .WithTitle("Informação")
                .WithColor(Color.Green)
                .WithFooter(footer => footer.WithText($"{time.Year} - Ares"));

            infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldModel), model.DisplayName);
            infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldRules), guild.GetTranslation(LangKeys.ChatDescriptionRules));
            infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldTime), guild.GetTranslation(LangKeys.ChatDescriptionTime));

            if (!string.IsNullOrWhiteSpace(model.DescriptionKey))
            {
                infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldDescription), guild.GetTranslation(model.DescriptionKey));
            }

            ComponentBuilder component = new ComponentBuilder();

            switch (model.Type)
            {
                case ModelType.Chat:
                    infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldHistory), guild.GetTranslation(LangKeys.HistoryChatDesc));
                    infoEmbed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionDefault));
                    break;

                case ModelType.Image:
                    infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldHistory), guild.GetTranslation(LangKeys.HistoryImageDesc));
                    infoEmbed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionImage));

                    /* 
                     * Quality Menu
                     */

                    SelectMenuBuilder qualityMenu = new SelectMenuBuilder()
                        .WithPlaceholder("Qualidade")
                        .WithCustomId("quality-menu");

                    Enum.GetValues(typeof(ImageQuality))
                        .Cast<ImageQuality>()
                        .ToList()
                        .ForEach(quality => qualityMenu.AddOption(new SelectMenuOptionBuilder()
                        {
                            Label = quality.ToString(),
                            Value = quality.ToString(),
                            IsDefault = quality.Equals(info.ImageGenOptions?.Quality)
                        }));

                    component.WithSelectMenu(qualityMenu);

                    /* 
                     * Size Menu
                     */

                    SelectMenuBuilder sizeMenu = new SelectMenuBuilder()
                        .WithPlaceholder("Tamanho")
                        .WithCustomId("size-menu");

                    Enum.GetValues(typeof(ImageSize))
                        .Cast<ImageSize>()
                        .ToList()
                        .ForEach(size => sizeMenu.AddOption(new SelectMenuOptionBuilder()
                        {
                            Label = size.ToString(),
                            Value = size.ToString(),
                            IsDefault = size.Equals(info.ImageGenOptions?.Size)
                        }));

                    component.WithSelectMenu(sizeMenu);

                    /* 
                     * Style Menu
                     */

                    SelectMenuBuilder styleMenu = new SelectMenuBuilder()
                        .WithPlaceholder("Estilo")
                        .WithCustomId("style-menu");

                    Enum.GetValues(typeof(ImageStyle))
                        .Cast<ImageStyle>()
                        .ToList()
                        .ForEach(style => styleMenu.AddOption(new SelectMenuOptionBuilder()
                        {
                            Label = style.ToString(),
                            Value = style.ToString(),
                            IsDefault = style.Equals(info.ImageGenOptions?.Style)
                        }));

                    component.WithSelectMenu(styleMenu);
                    break;

                case ModelType.TTS:
                    infoEmbed.AddField(guild.GetTranslation(LangKeys.FieldHistory), guild.GetTranslation(LangKeys.HistoryTTSDesc));
                    infoEmbed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionTTS));
                    break;

                default:
                    infoEmbed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionDefault));
                    break;
            }

            component.WithButton(new ButtonBuilder()
               .WithLabel(guild.GetTranslation(LangKeys.ButtonEndChat))
               .WithStyle(ButtonStyle.Danger)
               .WithCustomId("close-chat"));

            await channel.SendMessageAsync(embed: infoEmbed.Build(), components: component.Build());

            /*
             * Start:
             * Hello Message
             */

            EmbedBuilder helloEmbed = new EmbedBuilder()
                .WithTitle("Ares")
                .WithDescription(helloMessage)
                .WithColor(AresUtil.GetColorByModelCategory(model.Category))
                .WithFooter(footer => footer.WithText($"{time.Year} - Ares | {model.DisplayName}"));

            await channel.SendMessageAsync(embed: helloEmbed.Build());

            /*
             * End:
             * Hello Message
             */

            OverwritePermissions permissions = new OverwritePermissions(
                viewChannel: PermValue.Allow,
                readMessageHistory: PermValue.Allow,
                sendMessages: PermValue.Allow
            );

            await channel.AddPermissionOverwriteAsync(user, permissions);

            await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.SuccessChatCreated).Replace("{0}", channel.Mention));
        }
        catch (Exception e)
        {
            await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
            await AresLogger.ErrorAsync("SelectException", "Unable to process chat model choice.", e.Message);
        }
    }
}