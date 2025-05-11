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
using Ares.Core.Objects;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Image;
using Ares.Core.Objects.Model;
using Ares.Core.Repository;
using Ares.Core.Util;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class ChatImageOptionListener
{
    public ChatImageOptionListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
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

            await args.RespondAsync(ephemeral: true, text: AresConstant.LoadingEmote);
            RestInteractionMessage message = await args.GetOriginalResponseAsync();

            try
            {
                ulong? guildId = args.GuildId;

                if (!guildId.HasValue)
                {
                    await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                    return;
                }

                #region Check if guild is in database

                GuildRepository? guildRepository = AresCore.GuildRepository;

                if (guildRepository == null)
                {
                    await message.ModifyAsync(it => it.Content = $"{AresConstant.UnablePerformTask} (#g_repo_null)");
                    return;
                }

                Guild? guild = await guildRepository.FetchAsync(guildId.Value);

                const int maxAttempts = 3;

                for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    guild = await guildRepository.SaveAsync(guildId.Value);
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

                ulong? channelId = args.ChannelId;

                if (!channelId.HasValue)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.UnablePerformTask));
                    return;
                }

                SocketUser socketUser = args.User;

                UserChatInfo? chat = UserManager.ChatInfoByChannel(user, guildId.Value, channelId.Value);

                if (chat == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.CouldNotFindChat));
                    return;
                }

                ChatModelRepository? repository = AresCore.ChatModelRepository;

                if (repository == null)
                {
                    await message.ModifyAsync(it => it.Content = "Não foi possível encontrar os dados dos modelos.");
                    return;
                }

                ChatModel? model = await repository.FetchAsync(chat.ModelId, saveInRedis: true);

                if (model == null || model != null && model.Type != ModelType.Image)
                {
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.ChatIncompatibleOption));
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
                    await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.UnablePerformTask) + $" (#{optionValue})");
                    return;
                }

                chat.ImageGenOptions = options;
                await UserManager.UpdateChatInfoAsync(user, guildId.Value, chat);

                await message.ModifyAsync(it => it.Content = GuildManager.GetTranslation(guild, LangKeysConstant.Success));
            }
            catch (Exception e)
            {
                await args.FollowupAsync(AresConstant.UnablePerformTask);
                await AresLogger.LogAsync("ButtonException", "Unable to close chat.", e.Message, severity: Severity.Error);
            }
        });

        return Task.CompletedTask;
    }
}