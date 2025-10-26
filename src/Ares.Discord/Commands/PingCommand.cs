/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Discord;
using Discord.WebSocket;

namespace Ares.Discord.Commands;

public class PingCommand
{
    private static DiscordSocketClient? _client;

    public PingCommand(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += SlashCommandHandler;
        _client = client;
    }

    private Task SlashCommandHandler(SocketSlashCommand command)
    {
        _ = Task.Run(async () =>
        {
            if (_client == null || !command.Data.Name.Equals("ping")) return;
            
            if (Program.IsStarting || Program.IsShuttingDown) return;

            int ms = _client.Latency;

            Color color = ms < 30 ? Color.Green : ms >= 30 && ms <= 150 ? Color.Gold : ms > 150 ? Color.Red : Color.Default;

            Embed embed = new EmbedBuilder
            {
                Title = "Ping",
                Description = $"O ping do gateway atual é {ms}ms",
                Color = color,
            }
            .WithCurrentTimestamp()
            .Build();

            await command.RespondAsync(embed: embed, ephemeral: true);
        });

        return Task.CompletedTask;
    }
}