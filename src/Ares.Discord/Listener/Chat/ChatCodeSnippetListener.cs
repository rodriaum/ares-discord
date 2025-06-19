/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Constants;
using Ares.Core.Models.Chat.Historic;
using Ares.Core.Models.Data;
using Ares.Core.Objects;
using Ares.Core.Util;
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

                User? user = await _userService!.GetUser(args.User.Id, useCache: true);

                for (int attempts = maxAttempts; user == null && attempts > 0; attempts--)
                {
                    user = await _userService!.CreateOrGetUser(args.User.Id);
                }

                if (user == null)
                {
                    await args.RespondAsync(ephemeral: true, text: "Ops! Não foi possível criar a sua conta no banco de dados.");
                    return;
                }

                #endregion

                string id = args.Data.Values.FirstOrDefault("");

                UserChatSnippet? snippet = await _userService!.GetSnippetById(user.Id, guildId, id);

                if (snippet == null)
                {
                    await args.RespondAsync(ephemeral: true, text: "Snippet não encontrado ou não pode ser acessado por você.");
                    return;
                }

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