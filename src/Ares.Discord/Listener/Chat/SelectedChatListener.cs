/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Constants;
using Ares.Core.Manager.Database;
using Ares.Core.Models.Chat.Historic;
using Ares.Core.Models.Collection;
using Ares.Core.Models.Preference;
using Ares.Core.Objects;
using Ares.Core.Objects.Image;
using Ares.Core.Objects.Model;
using Ares.Core.Repository;
using Ares.Core.Util;
using Ares.Discord.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MongoDB.Driver;

namespace Ares.Discord.Listener.Chat;

public class SelectedChatListener
{
    private static DiscordSocketClient? _client { get; set; }

    public SelectedChatListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
        _client = client;
    }

    private Task SelectMenuHandler(SocketMessageComponent args)
    {
        _ = Task.Run(async () =>
        {
            if (!args.Data.CustomId.StartsWith("chat-menu-")) return;

            await args.RespondAsync(ephemeral: true, text: AresConstant.LoadingEmote);
            RestInteractionMessage message = await args.GetOriginalResponseAsync();

            try
            {
                SocketUser socketUser = args.User;
                ulong guildId = args.GuildId.GetValueOrDefault();

                if (_client == null || socketUser == null)
                {
                    await message.ModifyAsync(it => it.Content = AresConstant.UnableGetMember);
                    return;
                }

                #region Check if guild is in database

                GuildRepository? guildRepository = AresCore.GuildRepository;

                if (guildRepository == null)
                {
                    await message.ModifyAsync(it => it.Content = $"{AresConstant.UnablePerformTask} (#g_repo_null)");
                    return;
                }

                Guild? guild = await guildRepository.FetchAsync(guildId);

                const int maxAttempts = 3;

                for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    guild = await guildRepository.SaveAsync(guildId);
                }

                if (guild == null)
                {
                    await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a guilda no banco de dados.");
                    return;
                }

                #endregion

                #region Check if user is in database

                UserRepository? userRepository = AresCore.UserRepository;

                if (userRepository == null)
                {
                    await message.ModifyAsync(it => it.Content = $"{AresConstant.UnablePerformTask} (#u_repo_null)");
                    return;
                }

                User? user = await userRepository.FetchAsync(args.User.Id, saveInRedis: true);

                for (int attempts = maxAttempts; user == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar a sua conta no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    user = await userRepository.SaveAsync(args.User.Id);
                }

                if (user == null)
                {
                    await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a sua conta no banco de dados.");
                    return;
                }

                #endregion

                SocketGuild socketGuild = _client.GetGuild(guildId);

                if (socketGuild == null)
                {
                    await message.ModifyAsync(it => it.Content = AresConstant.UnableGetMember);
                    return;
                }

                GPreference? gid = guild.Preferences;

                if (gid == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.CouldNotFindInfoID));
                    return;
                }

                IRole usageRole = socketGuild.GetRole(gid.UsageRoleId);

                if (usageRole == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.RoleEliminated));
                    return;
                }

                SocketGuildUser member = socketGuild.GetUser(socketUser.Id);

                if (!member.Roles.Contains(usageRole))
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.RoleMissing).Replace("{0}", usageRole.Mention));
                    return;
                }

                IRole exclusiveRole = socketGuild.GetRole(gid.ExclusiveRoleId);

                if (exclusiveRole == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.RoleEliminated));
                    return;
                }

                if (UserManager.HasActiveUserConversation(user, guildId))
                {
                    bool isPremium = member.Roles.Contains(exclusiveRole);
                    int conversations = UserManager.GetConversationsCount(user, guildId, active: true);

                    if (!isPremium)
                    {
                        if (conversations >= AresConstant.MaxFreeConversations)
                        {
                            await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.ActiveConversation));
                            return;
                        }
                    }
                    else
                    {
                        if (conversations >= AresConstant.MaxPremiumConversations)
                        {
                            await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.PremiumChatLimit));
                            return;
                        }
                    }
                }

                ChatModelRepository? repository = AresCore.ChatModelRepository;

                if (repository == null)
                {
                    await message.ModifyAsync(it => it.Content = "Não foi possível encontrar os dados dos modelos.");
                    return;
                }

                ChatModel? model = await repository.FetchAsync(args.Data.Values.First(), saveInRedis: true);

                if (model == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.ModelNotFound));
                    return;
                }

                if (model.Dev && !AresCore.IsDeveloper(socketUser.Id))
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.ModelDevMode));
                    return;
                }

                if (!model.Available)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.ModelUnavailable));
                    return;
                }

                if (model.Exclusive && !member.Roles.Contains(exclusiveRole))
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.RoleMissing).Replace("{0}", exclusiveRole.Mention));
                    return;
                }

                string emojiUnicode = AresUtil.GetEmojiByModelType(model.Type).ToString();

                SocketCategoryChannel category = socketGuild.GetCategoryChannel(gid.ChatsCategoryId);
                RestTextChannel channel = await socketGuild.CreateTextChannelAsync($"{emojiUnicode}┃{socketUser.GlobalName}", properties => properties.CategoryId = category.Id);

                UserChatInfo info = new UserChatInfo
                    (
                        active: true,
                        channelId: channel.Id,
                        modelId: model.Id
                    );

                DateTime time = DateTime.Now;

                string greetingKey = (time.Hour >= 5 && time.Hour < 12) ? LangKeysConstant.GoodMorning :
                      (time.Hour >= 12 && time.Hour < 18) ? LangKeysConstant.GoodAfternoon :
                      LangKeysConstant.GoodNight;

                string helloMessage = string.Format(GuildManager.GetTranslation(guild, LangKeysConstant.HelloMessage), GuildManager.GetTranslation(guild, greetingKey), socketUser.GlobalName);

                UserChatHistoric historic = new UserChatHistoric(system: helloMessage);
                info.Historics.Add(historic);

                if (!await UserManager.CreateChatData(user, guildId, info))
                {
                    await channel.DeleteAsync();
                    await Task.Delay(500);
                    return;
                }

                EmbedBuilder infoEmbed = new EmbedBuilder()
                    .WithTitle("Informação")
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer.WithText($"{time.Year} - {AresConstant.AppName}"));

                infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldModel), model.DisplayName);
                infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldRules), GuildManager.GetTranslation(guild, LangKeysConstant.ChatDescriptionRules));
                infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldTime), GuildManager.GetTranslation(guild, LangKeysConstant.ChatDescriptionTime));

                if (!string.IsNullOrWhiteSpace(model.DescriptionKey))
                {
                    infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldDescription), GuildManager.GetTranslation(guild, model.DescriptionKey));
                }

                ComponentBuilder component = new ComponentBuilder();

                switch (model.Type)
                {
                    case ModelType.Chat:
                        infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldHistory), GuildManager.GetTranslation(guild, LangKeysConstant.HistoryChatDesc));
                        infoEmbed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.ChatDescriptionDefault));
                        break;

                    case ModelType.Image:
                        infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldHistory), GuildManager.GetTranslation(guild, LangKeysConstant.HistoryImageDesc));
                        infoEmbed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.ChatDescriptionImage));

                        #region Quality Menu

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

                        #endregion

                        #region Size Menu

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

                        #endregion

                        #region Style Menu

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

                        #endregion
                        break;

                    case ModelType.TTS:
                        infoEmbed.AddField(GuildManager.GetTranslation(guild, LangKeysConstant.FieldHistory), GuildManager.GetTranslation(guild, LangKeysConstant.HistoryTTSDesc));
                        infoEmbed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.ChatDescriptionTTS));
                        break;

                    default:
                        infoEmbed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.ChatDescriptionDefault));
                        break;
                }

                #region Close Chat Button

                component.WithButton(new ButtonBuilder()
                   .WithLabel(GuildManager.GetTranslation(guild, LangKeysConstant.ButtonEndChat))
                   .WithStyle(ButtonStyle.Danger)
                   .WithCustomId("close-chat"));

                #endregion

                #region User Chat Preference 

                SelectMenuBuilder preferenceMenu = new SelectMenuBuilder()
                    .WithPlaceholder("Preferências de Usuário (Não Funciona")
                    .WithCustomId($"user-chat-pref-{StringUtil.GenerateExclusiveCode(length: 8)}");

                preferenceMenu.AddOption(new SelectMenuOptionBuilder
                {
                    Label = $"Lembrar Histórico Passado",
                    Value = StringUtil.GenerateExclusiveCode(length: 24),
                    Emote = new Emoji("❌")
                });

                preferenceMenu.AddOption(new SelectMenuOptionBuilder
                {
                    Label = $"Mostrar Média de Valores",
                    Value = StringUtil.GenerateExclusiveCode(length: 24),
                    Emote = new Emoji("✅")
                });

                component.WithSelectMenu(preferenceMenu);

                #endregion

                await channel.SendMessageAsync(embed: infoEmbed.Build(), components: component.Build());

                #region Hello Message

                EmbedBuilder helloEmbed = new EmbedBuilder()
                    .WithTitle("Ares")
                    .WithDescription(helloMessage)
                    .WithColor(AresUtil.GetColorByModelCategory(model.Category))
                    .WithFooter(footer => footer.WithText($"{time.Year} - {AresConstant.AppName} | {model.DisplayName}"));

                await channel.SendMessageAsync(embed: helloEmbed.Build());

                #endregion

                OverwritePermissions permissions = new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    sendMessages: PermValue.Allow
                );

                await channel.AddPermissionOverwriteAsync(socketUser, permissions);

                await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.SuccessChatCreated).Replace("{0}", channel.Mention));
                await Task.Delay(TimeSpan.FromSeconds(5));
                await message.DeleteAsync();
            }
            catch (Exception e)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                await AresLogger.LogAsync("SelectException", "Unable to process chat model choice.", e.Message, severity: Severity.Error);
            }
        });

        return Task.CompletedTask;
    }
}