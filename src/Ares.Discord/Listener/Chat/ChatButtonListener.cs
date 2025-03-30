/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Database.Collection;
using Ares.Core.Database.Model;
using Ares.Core.Database.Model.Chat.Sub;
using Ares.Core.Objects.Language;
using Ares.Core.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

internal class ChatButtonListener
{
    public ChatButtonListener(DiscordSocketClient client)
    {
        client.ButtonExecuted += ButtonExecutedHandler;
    }

    private async Task ButtonExecutedHandler(SocketMessageComponent args)
    {
        if (!(
            args.Data.CustomId.Equals("close-chat") ||
            args.Data.CustomId.Equals("quality-menu") ||
            args.Data.CustomId.Equals("style-menu") ||
            args.Data.CustomId.Equals("size-menu")
        )) return;

        await args.RespondAsync(ephemeral: true);
        RestInteractionMessage message = await args.GetOriginalResponseAsync();

        try
        {
            ulong? guildId = args.GuildId;

            if (!guildId.HasValue)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                return;
            }

            GuildCollection? data = AresCore.GuildCollection;

            if (data == null)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                return;
            }

            Guild? guild = await data.FetchAsync(guildId.Value);
            const int maxAttempts = 3;

            for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
            {
                await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                await Task.Delay(1500);
                guild = await data.SaveAsync(guildId.Value);
            }

            if (guild == null)
            {
                await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar o servidor no banco de dados.");
                return;
            }

            IUser user = args.User;

            switch (args.Data.CustomId)
            {
                /*
                 * General chats
                 */

                case "close-chat":
                    var channel = await args.GetChannelAsync() as SocketTextChannel;

                    if (channel != null)
                    {
                        if (!await guild.ToggleChatInfo(user, channel.Id, false))
                        {
                            await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                            return;
                        }

                        GChatInfoModel? info = guild.ChatInfoByChannel(user, channel.Id);

                        if (info == null)
                        {
                            await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.NotChatOwner));
                            return;
                        }

                        await args.FollowupAsync(guild.GetTranslation(LangKeys.CloseChat));

                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await channel.DeleteAsync();

                        AresLogger.Log("Chat", $"Chat \"{info.Id}\" disabled by \"{user.Username}\"");
                    }
                    else
                    {
                        await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                    }
                    break;

                /*
                 * Image generation chat
                 */

                case "quality-menu":
                    break;

                case "style-menu":
                    break;

                case "size-menu":
                    break;
            }
        }
        catch (Exception e)
        {
            await args.FollowupAsync(AresConstant.UnablePerformTask);
            await AresLogger.ErrorAsync("ButtonException", "Unable to close chat.", e.Message);
        }
    }
}