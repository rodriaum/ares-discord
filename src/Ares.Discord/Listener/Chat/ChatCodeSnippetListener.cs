/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Models.Chat;
using Ares.Core.Util;
using Discord;
using Discord.Rest;
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
            if (!args.Data.CustomId.StartsWith("snippet-")) return;

            await args.RespondAsync(ephemeral: true, text: AresConstant.LoadingEmote);
            RestInteractionMessage message = await args.GetOriginalResponseAsync();

            try
            {
                SocketUser user = args.User;
                ulong guildId = args.GuildId.GetValueOrDefault();

                if (_client == null || user == null)
                {
                    await message.ModifyAsync(it => it.Content = AresConstant.UnableGetMember);
                    return;
                }

                ChatCodeSnippet? snippet = Program.CodeSnippets.Find(it => it.UserId == user.Id && it.MessageId == args.Data.CustomId);

                if (snippet == null)
                {
                    await message.ModifyAsync(it => it.Content = "Snippet não encontrado.");
                    return;
                }

                ModalBuilder modal = new ModalBuilder()
                    .WithTitle("Código")
                    .AddTextInput(
                        label: "Código",
                        customId: $"modal-code-snippet-{args.Id}",
                        style: TextInputStyle.Paragraph,
                        placeholder: "Código",
                        value: snippet.Code ?? "Nada a apresentar..."
                    );

                await args.RespondWithModalAsync(modal.Build());

            }
            catch (Exception e)
            {
                await message.ModifyAsync(it => it.Content = AresConstant.UnablePerformTask);
                await AresLogger.ErrorAsync("SelectException", "Unable to process chat code snippet.", e.Message);
            }
        });

        return Task.CompletedTask;
    }
}