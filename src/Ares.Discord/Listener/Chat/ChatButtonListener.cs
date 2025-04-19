/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Models;
using Ares.Core.Models.Chat.Sub;
using Ares.Core.Objects.Language;
using Ares.Core.Repository;
using Ares.Core.Service;
using Ares.Core.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class ChatButtonListener
{
    public ChatButtonListener(DiscordSocketClient client)
    {
        client.ButtonExecuted += ButtonExecutedHandler;
    }

    private Task ButtonExecutedHandler(SocketMessageComponent args)
    {
        _ = Task.Run(async () =>
        {
            if (!args.Data.CustomId.Equals("close-chat")) return;

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

                SocketTextChannel? channel = await args.GetChannelAsync() as SocketTextChannel;

                if (channel == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.UnablePerformTask));
                    return;
                }

                IUser user = args.User;

                if (!await GuildService.ToggleChatInfo(guild, user.Id, channel.Id, false))
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.UnableFindChat));
                    return;
                }

                GChatInfo? info = GuildService.ChatInfoByChannel(guild, user.Id, channel.Id);

                if (info == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.NotChatOwner));
                    return;
                }

                AresLogger.Log("Chat", $"Chat \"{info.Id}\" eliminated by \"{user.Username}\"");

                await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.CloseChat));

                await Task.Delay(TimeSpan.FromSeconds(1));
                await channel.DeleteAsync();
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