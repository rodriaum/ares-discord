using Discord.WebSocket;

namespace Ares.src.Commands.Data
{
    internal class ConfigCommand
    {
        private static DiscordSocketClient? Client;

        public ConfigCommand(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;
            Client = client;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Guild.Guild? guild = await Core.GuildData.Fetch("1270010171116224562");

            // terminar aqui

            guild.SetField(guildIdData: new GuildIdData
            {
                MemberRoleId = 1270034491301302293,
                UsageRoleId = 1270726265497980960,
                ExclusiveRoleId = 1270600361509388368,
                SetupChannelId = 1270562665311240263,
                ChatsCategoryId = 1270562537498476758
            });
        }
    }
}
