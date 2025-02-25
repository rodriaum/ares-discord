using Ares.src.Guild.Config;
using Discord;
using Discord.WebSocket;

namespace Ares.src.Commands.Data
{
    internal class TokenConfigCommand
    {
        private static DiscordSocketClient? Client;

        public TokenConfigCommand(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;
            Client = client;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (Client == null || !command.Data.Name.Equals("config-token")) return;

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Configuração de Token")
                .WithDescription("Aguarde...")
                .WithColor(Color.Gold)
                .WithFooter("Ares");

            await command.FollowupAsync(embed: embed.Build());

            GuildData? data = Core.GuildData;

            if (data != null)
            {
                await command.FollowupAsync(
                    embed: embed
                        .WithDescription("Não foi encontrado nenhum problema com as informações...")
                        .Build()
                );
            }
            else
            {
                await command.FollowupAsync(
                    embed: embed
                        .WithDescription("Não foi possível acessar as informações do servidor atual.")
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            ulong? guildId = command.GuildId;

            if (guildId == null)
            {
                await command.FollowupAsync(
                    embed: embed
                        .WithDescription("Não foi possível encontrar o ID do servidor atual.")
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
            }

            Guild.Guild? guild = await data.Fetch(guildId.Value);

            

        }
    }
}