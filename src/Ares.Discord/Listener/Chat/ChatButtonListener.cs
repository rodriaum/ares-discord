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

        await args.RespondAsync(ephemeral: true, text: ":hourglass:");
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
                await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a guilda no banco de dados.");
                return;
            }

            SocketTextChannel? channel = await args.GetChannelAsync() as SocketTextChannel;

            if (channel == null)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                return;
            }

            IUser user = args.User;

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

            AresLogger.Log("Chat", $"Chat \"{info.Id}\" eliminated by \"{user.Username}\"");

            await message.ModifyAsync(it => it.Content = guild.GetTranslation(LangKeys.CloseChat));

            await Task.Delay(TimeSpan.FromSeconds(1));
            await channel.DeleteAsync();
        }
        catch (Exception e)
        {
            await args.FollowupAsync(AresConstant.UnablePerformTask);
            await AresLogger.ErrorAsync("ButtonException", "Unable to close chat.", e.Message);
        }
    }
}