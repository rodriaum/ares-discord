using Discord.WebSocket;

namespace Discord_OpenAI.commands
{
    internal class PingCommand
    {
        public PingCommand(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (!command.Data.Name.Equals("ping")) return;

            await command.RespondAsync($"You executed {command.Data.Name}");
        }
    }
}