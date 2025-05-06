/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Models;
using Ares.Core.Models.Chat.Sub;
using Ares.Core.Models.Collection;
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

                SocketTextChannel? channel = await args.GetChannelAsync() as SocketTextChannel;

                if (channel == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.UnablePerformTask));
                    return;
                }

                SocketUser socketUser = args.User;

                if (!await UserService.ToggleChatInfo(user, guild.Id, channel.Id, false))
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.UnableFindChat));
                    return;
                }

                GChatInfo? info = UserService.ChatInfoByChannel(user, guild.Id, channel.Id);

                if (info == null)
                {
                    await message.ModifyAsync(it => it.Content = GuildService.GetTranslation(guild, LangKeys.NotChatOwner));
                    return;
                }

                // Removes the generated conversation snippets as they will no longer be used in that channel.
                await UserService.RemoveSnippetByChannelAsync(user, guild.Id, channel.Id);

                AresLogger.Log("Chat", $"Chat \"{info.Id}\" eliminated by \"{user.Id}\"");

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