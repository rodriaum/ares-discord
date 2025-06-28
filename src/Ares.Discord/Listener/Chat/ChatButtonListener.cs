/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.DTOs;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Common.Util;
using Ares.Discord.Service.Neural;
using Ares.Discord.Services.Api;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class ChatButtonListener
{
    private static GuildService? _guildService { get; set; }
    private static UserService? _userService { get; set; }

    public ChatButtonListener(DiscordSocketClient client)
    {
        client.ButtonExecuted += ButtonExecutedHandler;

        _guildService = Program.GuildService;
        _userService = Program.UserService;

        if (_guildService == null || _userService == null)
        {
            AresLogger.Log(nameof(NeuralService), "Guild or User service is not initialized.", severity: Severity.Error);
            throw new InvalidOperationException("Guild or User service is not initialized.");
        }
    }

    private Task ButtonExecutedHandler(SocketMessageComponent args)
    {
        _ = Task.Run(async () =>
        {
            if (!args.Data.CustomId.Equals("close-chat")) return;

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

                ApiResult<Guild>? guildResult = await _guildService!.GetGuild(guildId.Value);

                if (guildResult == null || !guildResult.Success)
                {
                    await message.ModifyAsync(it => it.Content = "Não foi possível acessar as informações do servidor atual.");
                    return;
                }

                Guild? guild = guildResult.Data;

                const int maxAttempts = 3;

                for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar guilda no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);
                    ApiResult<Guild>? createResult = await _guildService!.CreateOrGetGuild(guildId.Value);
                    if (createResult != null && createResult.Success)
                        guild = createResult.Data;
                }

                if (guild == null)
                {
                    await message.ModifyAsync(it => it.Content = "Ops! Não foi possível criar a guilda no banco de dados.");
                    return;
                }

                #endregion

                #region Check if user is in database

                ApiResult<User>? userResult = await _userService!.GetUser(args.User.Id, useCache: true);
                User? user = (userResult != null && userResult.Success) ? userResult.Data : null;

                for (int attempts = maxAttempts; user == null && attempts > 0; attempts--)
                {
                    await message.ModifyAsync(it => it.Content = $"A tentar criar a sua conta no banco de dados... {attempts}/{maxAttempts}");
                    await Task.Delay(1500);

                    ApiResult<User>? createUserResult = await _userService.CreateOrGetUser(args.User.Id);
                    if (createUserResult != null && createUserResult.Success)
                        user = createUserResult.Data;
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
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.UnablePerformTask));
                    return;
                }

                SocketUser socketUser = args.User;

                ApiResult<bool>? isOwnerResult = await _userService!.IsChatOwner(user.Id, guild.Id, channel.Id);
                if (isOwnerResult == null || !isOwnerResult.Success || !(isOwnerResult.Data))
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.NotChatOwner));
                    return;
                }

                ApiResult<bool>? toggleResult = await _userService!.ToggleChatStatus(user.Id, guild.Id, channel.Id, false);
                if (toggleResult == null || !toggleResult.Success || !(toggleResult.Data))
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.UnableFindChat) + " (toggle_chat_info)");
                    return;
                }

                ApiResult<UserChatInfo>? infoResult = await _userService.GetChatInfoByChannel(user.Id, guild.Id, channel.Id);

                if (infoResult == null || !infoResult.Success || infoResult.Data == null)
                {
                    await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.UnableFindChat) + " (info_null)");
                    return;
                }

                // Remove os snippets gerados pois não serão mais usados nesse canal.
                await _userService!.RemoveSnippetsByChannel(user.Id, guild.Id, channel.Id);

                AresLogger.Log("Chat", $"Chat \"{infoResult.Data.Id}\" eliminado por \"{user.Id}\"", severity: Severity.Info);

                await message.ModifyAsync(it => it.Content = Program.LangManager.GetTranslation(guild, LanguageKeys.CloseChat));

                await Task.Delay(TimeSpan.FromSeconds(1));
                await channel.DeleteAsync();
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