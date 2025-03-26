using Discord;
using Discord.WebSocket;

namespace Ares.Commands;

internal class PingCommand
{
    private static DiscordSocketClient? Client;

    public PingCommand(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += SlashCommandHandler;
        Client = client;
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (Client == null || !command.Data.Name.Equals("ping")) return;
        
            int ms = Client.Latency;

            Color color = ms < 30 ? Color.Green : ms >= 30 && ms <= 150 ? Color.Gold : ms > 150 ? Color.Red : Color.Default;

            Embed embed = new EmbedBuilder
            {
                Title = "Ping",
                Description = $"O ping do gateway atual é {Client.Latency}ms",
                Color = color,
            }
            .WithCurrentTimestamp()
            .Build();

            await command.RespondAsync(embed: embed, ephemeral: true);        }
}