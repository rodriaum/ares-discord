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
        if (!args.Data.CustomId.Equals("close-chat")) return;

        await args.RespondAsync(ephemeral: true);
        RestInteractionMessage message = await args.GetOriginalResponseAsync();

        try
        {
            IUser user = args.User;

            if (user == null)
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

            Guild? guild = await data.FetchAsync(args.GuildId.GetValueOrDefault());

            if (guild == null)
            {
                await message.ModifyAsync(it => it.Content = "Ops! Parece que o servidor atual não foi configurado no banco de dados.");
                return;
            }

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
                    await message.ModifyAsync(it => it.Content = "Parece que você não é o proprietário desse canal.");
                    return;
                }

                await args.FollowupAsync(guild.GetTranslation(LangKeys.CloseChat));

                await Task.Delay(TimeSpan.FromSeconds(1));
                await channel.DeleteAsync();

                AresLogger.Log("Chat", $"Chat \"{info.Id}\" disabled by \"{user.Username}#{user.Discriminator}\"");
            }
            else
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
            }
        }
        catch (Exception e)
        {
            await args.FollowupAsync(AresConstant.UnablePerformTask);
            await AresLogger.ErrorAsync("ButtonException", "Unable to close chat.", e.Message);
        }
    }
}