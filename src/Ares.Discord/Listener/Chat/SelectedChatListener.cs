/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Data;
using Ares.Common.Models.Preference;
using Ares.Common.Objects;
using Ares.Common.Objects.Image;
using Ares.Common.Util;
using Ares.Discord.Service.Neural;
using Ares.Discord.Services.Api;
using Ares.Discord.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class SelectedChatListener
{
    private static DiscordSocketClient? _client { get; set; }

    private static GuildService? _guildService { get; set; }
    private static UserService? _userService { get; set; }
    private static ChatModelService? _chatModelService { get; set; }

    public SelectedChatListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
        _client = client;

        _guildService = Program.GuildService;
        _userService = Program.UserService;
        _chatModelService = Program.ChatModelService;

        if (_guildService == null || _userService == null || _chatModelService == null)
        {
            AresLogger.Log(nameof(NeuralService), "Guild, User or ChatModel service is not initialized.", severity: Severity.Error);
            throw new InvalidOperationException("Guild, User or ChatModel service is not initialized.");
        }
    }

    private Task SelectMenuHandler(SocketMessageComponent args)
    {
        _ = Task.Run(async () =>
        {
            if (!args.Data.CustomId.StartsWith("chat-menu-")) return;

            await args.RespondAsync(ephemeral: true, text: AppConstants.LoadingEmote);
            RestInteractionMessage message = await args.GetOriginalResponseAsync();

            try
            {
                SocketUser socketUser = args.User;
                ulong guildId = args.GuildId.GetValueOrDefault();

                if (_client == null || socketUser == null)
                {
                    await message.ModifyAsync(it => it.Content = AppConstants.UnableGetMember);
                    return;
                }

                #region Check if guild is in database

                Guild? guild = await _guildService!.GetGuild(guildId);

                const int maxAttempts = 3;

                for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    guild = await _guildService!.CreateOrGetGuild(guildId);
                }

                if (guild == null)
                {
                    await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a guilda no banco de dados.");
                    return;
                }

                #endregion

                #region Check if user is in database

                User? user = await _userService!.GetUser(args.User.Id, useCache: true);

                for (int attempts = maxAttempts; user == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar a sua conta no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    user = await _userService!.CreateOrGetUser(args.User.Id);
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
                    await message.ModifyAsync(it => it.Content = AppConstants.UnableGetMember);
                    return;
                }

                GPreference? gid = guild.Preferences;

                if (gid == null)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindInfoID));
                    return;
                }

                IRole usageRole = socketGuild.GetRole(gid.UsageRoleId);

                if (usageRole == null)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.RoleEliminated));
                    return;
                }

                SocketGuildUser member = socketGuild.GetUser(socketUser.Id);

                if (!member.Roles.Contains(usageRole))
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.RoleMissing).Replace("{0}", usageRole.Mention));
                    return;
                }

                IRole exclusiveRole = socketGuild.GetRole(gid.ExclusiveRoleId);

                if (exclusiveRole == null)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.RoleEliminated));
                    return;
                }

                bool? hasActiveConversation = await _userService!.HasActiveConversation(user.Id, guildId);

                if (hasActiveConversation == null)
                {
                    await message.ModifyAsync(it => it.Content = "Ops! Não foi possível verificar se você tem uma conversa ativa.");
                    return;
                }

                if (hasActiveConversation.Value)
                {
                    bool isPremium = member.Roles.Contains(exclusiveRole);
                    int? conversations = await _userService.GetConversationCount(user.Id, guildId, activeOnly: true);

                    if (conversations == null)
                    {
                        await message.ModifyAsync(it => it.Content = "Ops! Não foi possível verificar o número de conversas ativas.");
                        return;
                    }

                    if (!isPremium)
                    {
                        if (conversations >= AppConstants.MaxFreeConversations)
                        {
                            await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.ActiveConversation));
                            return;
                        }
                    }
                    else
                    {
                        if (conversations >= AppConstants.MaxPremiumConversations)
                        {
                            await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.PremiumChatLimit));
                            return;
                        }
                    }
                }

                ChatModel? model = await _chatModelService!.GetModel(args.Data.Values.First(), saveInRedis: true);

                if (model == null)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.ModelNotFound));
                    return;
                }

                if (model.Dev && !Program.IsDeveloper(socketUser.Id))
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.ModelDevMode));
                    return;
                }

                if (!model.Available)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.ModelUnavailable));
                    return;
                }

                if (model.Exclusive && !member.Roles.Contains(exclusiveRole))
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.RoleMissing).Replace("{0}", exclusiveRole.Mention));
                    return;
                }

                string emojiUnicode = AresUtil.GetEmojiByModelType(model.Type).ToString();

                SocketCategoryChannel category = socketGuild.GetCategoryChannel(gid.ChatsCategoryId);
                RestTextChannel channel = await socketGuild.CreateTextChannelAsync($"{emojiUnicode}┃{socketUser.GlobalName}", properties => properties.CategoryId = category.Id);

                UserChatInfo info = new UserChatInfo
                    (
                        active: true,
                        channelId: channel.Id,
                        modelId: model.Id,
                        // Warn: If this is not an type of image this need to be null, to another systems run correctly.
                        imageGenOptions: (model.Type == ModelType.Image ? new() : null)
                    );

                DateTime time = DateTime.Now;

                string greetingKey = (time.Hour >= 5 && time.Hour < 12) ? LanguageKeys.GoodMorning :
                      (time.Hour >= 12 && time.Hour < 18) ? LanguageKeys.GoodAfternoon :
                      LanguageKeys.GoodNight;

                string helloMessage = string.Format(Program.LangManager.GetTranslation(guild, LanguageKeys.HelloMessage), Program.LangManager.GetTranslation(guild, greetingKey), socketUser.GlobalName);

                UserChatHistoric historic = new UserChatHistoric(system: helloMessage);
                info.Historics.Add(historic);

//#error Erro ao tentar converter resultado de "message" para string.
                if (!await _userService.CreateChatData(user.Id, guildId, info))
                {
                    await channel.DeleteAsync();
                    await Task.Delay(500);
                    return;
                }

                EmbedBuilder infoEmbed = new EmbedBuilder()
                    .WithTitle("Informação")
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer.WithText($"{time.Year} - {AppConstants.AppName}"));

                infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldModel), model.DisplayName);
                infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldRules), Program.LangManager.GetTranslation(guild, LanguageKeys.ChatDescriptionRules));
                infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldTime), Program.LangManager.GetTranslation(guild, LanguageKeys.ChatDescriptionTime));

                if (!string.IsNullOrWhiteSpace(model.DescriptionKey))
                {
                    infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldDescription), Program.LangManager.GetTranslation(guild, model.DescriptionKey));
                }

                ComponentBuilder component = new ComponentBuilder();

                switch (model.Type)
                {
                    case ModelType.Chat:
                        infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldHistory), Program.LangManager.GetTranslation(guild, LanguageKeys.HistoryChatDesc));
                        infoEmbed.WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.ChatDescriptionDefault));
                        break;

                    case ModelType.Image:
                        infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldHistory), Program.LangManager.GetTranslation(guild, LanguageKeys.HistoryImageDesc));
                        infoEmbed.WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.ChatDescriptionImage));

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
                        infoEmbed.AddField(Program.LangManager.GetTranslation(guild, LanguageKeys.FieldHistory), Program.LangManager.GetTranslation(guild, LanguageKeys.HistoryTTSDesc));
                        infoEmbed.WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.ChatDescriptionTTS));
                        break;

                    default:
                        infoEmbed.WithDescription(Program.LangManager.GetTranslation(guild, LanguageKeys.ChatDescriptionDefault));
                        break;
                }

                #region Close Chat Button

                component.WithButton(new ButtonBuilder()
                   .WithLabel(Program.LangManager.GetTranslation(guild, LanguageKeys.ButtonEndChat))
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
                    .WithFooter(footer => footer.WithText($"{time.Year} - {AppConstants.AppName} | {model.DisplayName}"));

                await channel.SendMessageAsync(embed: helloEmbed.Build());

                #endregion

                OverwritePermissions permissions = new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    sendMessages: PermValue.Allow
                );

                await channel.AddPermissionOverwriteAsync(socketUser, permissions);

                await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.SuccessChatCreated).Replace("{0}", channel.Mention));
                await Task.Delay(TimeSpan.FromSeconds(5));
                await message.DeleteAsync();
            }
            catch (Exception e)
            {
                await message.ModifyAsync(it => it.Content = AppConstants.UnablePerformTask);
                await AresLogger.LogAsync("SelectException", "Unable to process chat model choice.", severity: Severity.Error, extra: e.Message);
            }
        });

        return Task.CompletedTask;
    }
}