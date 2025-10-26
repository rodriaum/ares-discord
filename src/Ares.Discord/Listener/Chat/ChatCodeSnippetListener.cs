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
using Discord;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class ChatCodeSnippetListener
{
    private static DiscordSocketClient? _client { get; set; }

    private static GuildService? _guildService { get; set; }
    private static UserService? _userService { get; set; }

    public ChatCodeSnippetListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
        _client = client;

        _guildService = Program.GuildService;
        _userService = Program.UserService;

        if (_guildService == null || _userService == null)
        {
            AresLogger.Log(nameof(NeuralService), "Guild or User service is not initialized.", severity: Severity.Error);
            throw new InvalidOperationException("Guild or User service is not initialized.");
        }
    }

    private Task SelectMenuHandler(SocketMessageComponent args)
    {
        _ = Task.Run(async () =>
        {
            if (!args.Data.CustomId.StartsWith("chat-snippet-")) return;

            if (Program.IsStarting || Program.IsShuttingDown) return;
            
            try
            {
                SocketUser socketUser = args.User;
                ulong guildId = args.GuildId.GetValueOrDefault();

                if (_client == null || socketUser == null)
                {
                    await args.RespondAsync(ephemeral: true, text: AppConstants.UnableGetMember);
                    return;
                }

                #region Check if user is in database

                int maxAttempts = 3;

                ApiResult<User>? userResult = await _userService!.GetUser(args.User.Id, useCache: true);
                User? user = (userResult != null && userResult.Success) ? userResult.Data : null;

                for (int attempts = maxAttempts; user == null && attempts > 0; attempts--)
                {
                    ApiResult<User>? createUserResult = await _userService!.CreateOrGetUser(args.User.Id);
                    if (createUserResult != null && createUserResult.Success)
                        user = createUserResult.Data;
                }

                if (user == null)
                {
                    await args.RespondAsync(ephemeral: true, text: "Ops! Não foi possível criar a sua conta no banco de dados.");
                    return;
                }

                #endregion

                string id = args.Data.Values.FirstOrDefault("");

                ApiResult<UserChatSnippet>? snippetResult = await _userService!.GetSnippetById(user.Id, guildId, id);
                if (snippetResult == null || !snippetResult.Success || snippetResult.Data == null)
                {
                    await args.RespondAsync(ephemeral: true, text: "Snippet não encontrado ou não pode ser acessado por você.");
                    return;
                }

                UserChatSnippet snippet = snippetResult.Data;

                string code = StringUtil.GenerateExclusiveCode(length: 4);

                ModalBuilder modal = new ModalBuilder()
                    .WithTitle("Visualizador")
                    .WithCustomId($"modal-chat-snippet-{code}")
                    .AddTextInput(
                        label: "Trecho",
                        customId: $"input-chat-snippet-{code}",
                        style: TextInputStyle.Paragraph,
                        value: snippet.Text ?? "Nada a apresentar..."
                    );

                await args.RespondWithModalAsync(modal.Build());

            }
            catch (Exception e)
            {
                try
                {
                    await args.RespondAsync(ephemeral: true, text: AppConstants.UnablePerformTask);
                }
                catch { }

                await AresLogger.LogAsync("SelectException", "Unable to process chat code snippet.", severity: Severity.Error, extra: e.Message);
            }
        });

        return Task.CompletedTask;
    }
}