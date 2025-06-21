/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Chat.Image;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Common.Objects.Image;
using Ares.Common.Util;
using Ares.Discord.Service.Neural;
using Ares.Discord.Services.Api;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class ChatImageOptionListener
{
    private static GuildService? _guildService { get; set; }
    private static UserService? _userService { get; set; }
    private static ChatModelService? _chatModelService { get; set; }

    public ChatImageOptionListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;

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
            if (!(
                args.Data.CustomId.Equals("quality-menu") ||
                args.Data.CustomId.Equals("style-menu") ||
                args.Data.CustomId.Equals("size-menu")
            )) return;

            await args.RespondAsync(ephemeral: true, text: AppConstants.LoadingEmote);
            RestInteractionMessage message = await args.GetOriginalResponseAsync();

            try
            {
                ulong? guildId = args.GuildId;

                if (!guildId.HasValue)
                {
                    await message.ModifyAsync(it => it.Content = AppConstants.UnablePerformTask);
                    return;
                }

                #region Check if guild is in database

                Guild? guild = await _guildService!.GetGuild(guildId.Value);

                const int maxAttempts = 3;

                for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    guild = await _guildService!.CreateOrGetGuild(guildId.Value);
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

                ulong? channelId = args.ChannelId;

                if (!channelId.HasValue)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.UnablePerformTask));
                    return;
                }

                SocketUser socketUser = args.User;

                UserChatInfo? chat = await _userService!.GetChatInfoByChannel(user.Id, guildId.Value, channelId.Value);

                if (chat == null)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.CouldNotFindChat));
                    return;
                }

                ChatModel? model = await _chatModelService!.GetModel(chat.ModelId, saveInRedis: true);

                if (model == null || model != null && model.Type != ModelType.Image)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.ChatIncompatibleOption));
                    return;
                }

                ImageGenOptions options = chat.ImageGenOptions ?? new ImageGenOptions();

                string optionValue = args.Data.Values.First();
                bool success = false;

                switch (args.Data.CustomId)
                {
                    case "quality-menu":
                        if (Enum.TryParse(optionValue, true, out ImageQuality quality))
                        {
                            options.Quality = quality;
                            success = true;
                        }
                        break;

                    case "style-menu":
                        if (Enum.TryParse(optionValue, true, out ImageStyle style))
                        {
                            options.Style = style;
                            success = true;
                        }
                        break;

                    case "size-menu":
                        if (Enum.TryParse(optionValue, true, out ImageSize size))
                        {
                            options.Size = size;
                            success = true;
                        }
                        break;
                }

                if (!success)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.UnablePerformTask) + $" (#{optionValue})");
                    return;
                }

                chat.ImageGenOptions = options;
                await _userService.UpdateChatInfo(user.Id, guildId.Value, chat);

                await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.Success));
            }
            catch (Exception e)
            {
                await args.FollowupAsync(AppConstants.UnablePerformTask);
                await AresLogger.LogAsync("ButtonException", "Unable to close chat.", severity: Severity.Error, extra: e.Message);
            }
        });

        return Task.CompletedTask;
    }
}