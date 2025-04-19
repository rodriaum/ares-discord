/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Models;
using Ares.Core.Models.Chat.Sub;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Repository;
using Ares.Core.Service;
using Ares.Core.Util;
using Discord;
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

                GuildRepository? repository = AresCore.GRepository;

                if (repository == null)
                {
                    await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                    return;
                }

                Guild? guild = await repository.FetchAsync(guildId.Value);
                const int maxAttempts = 3;

                for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    guild = await repository.SaveAsync(guildId.Value);
                }

                if (guild == null)
                {
                    await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a guilda no banco de dados.");
                    return;
                }

                ulong? channelId = args.ChannelId;

                if (!channelId.HasValue)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.UnablePerformTask));
                    return;
                }

                IUser user = args.User;

                GChatInfo? chat = GuildService.ChatInfoByChannel(guild, user.Id, channelId.Value);

                if (chat == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.CouldNotFindChat));
                    return;
                }

                ChatModel? model = ChatModel.GetByModel(chat.Model);

                if (model == null || model != null && model.Type != ModelType.Image)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.ChatIncompatibleOption));
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
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.UnablePerformTask) + $" (#{optionValue})");
                    return;
                }

                chat.ImageGenOptions = options;
                await GuildService.UpdateChatInfoAsync(guild, user.Id, chat);

                await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.Success));
            }
            catch (Exception e)
            {
                await args.FollowupAsync(AresConstant.UnablePerformTask);
                await AresLogger.ErrorAsync("ButtonException", "Unable to close chat.", e.Message);
            }
        });

        return Task.CompletedTask;
    }
}