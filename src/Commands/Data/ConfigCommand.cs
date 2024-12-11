using Ares.src.Guild.ChatData;
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
            Guild.Guild? guild = await Core.GuildData?.Fetch("1277819529602400306");

            // terminar aqui


            if (guild != null)
            {
                await guild.SaveGuildIdData(
                    new GuildIdData
                    {
                        MemberRoleId = 1277819529602400313,
                        UsageRoleId = 1277819529619308561,
                        ExclusiveRoleId = 1277819529619308562,
                        SetupChannelId = 1277819529979756634,
                        ChatsCategoryId = 1316513138845286499
                    });
            }
        }
    }
}