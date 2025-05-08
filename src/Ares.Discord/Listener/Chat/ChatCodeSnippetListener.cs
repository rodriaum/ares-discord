/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Manager;
using Ares.Core.Models;
using Ares.Core.Models.Chat.Sub;
using Ares.Core.Models.Collection;
using Ares.Core.Repository;
using Ares.Core.Util;
using Discord;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

public class ChatCodeSnippetListener
{
    private static DiscordSocketClient? _client { get; set; }

    public ChatCodeSnippetListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
        _client = client;
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
                    await args.RespondAsync(ephemeral: true, text: AresConstant.UnableGetMember);
                    return;
                }

                #region Check if user is in database

                UserRepository? userRepository = AresCore.UserRepository;

                int maxAttempts = 3;

                if (userRepository == null)
                {
                    await args.RespondAsync(ephemeral: true, text: $"{AresConstant.UnablePerformTask} (#u_repo_null)");
                    return;
                }

                User? user = await userRepository.FetchAsync(args.User.Id, saveInRedis: true);

                for (int attempts = maxAttempts; user == null && attempts > 0; attempts--)
                {
                    user = await userRepository.SaveAsync(args.User.Id);
                }

                if (user == null)
                {
                    await args.RespondAsync(ephemeral: true, text: "Ops! Não foi possível criar a sua conta no banco de dados.");
                    return;
                }

                #endregion

                UserChatSnippet? snippet = UserManager.GetSnippet(user, guildId, args.Data.CustomId);

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
                    await args.RespondAsync(ephemeral: true, text: AresConstant.UnablePerformTask);
                }
                catch { }

                await AresLogger.LogAsync("SelectException", "Unable to process chat code snippet.", e.Message, severity: Severity.Error);
            }
        });

        return Task.CompletedTask;
    }
}