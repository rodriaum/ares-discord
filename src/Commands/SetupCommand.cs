using Discord;
using Discord.WebSocket;
using Ares.src.Service;
using Ares.src.Service.Model;
using Ares.src.Manager;

namespace Ares.src.Commands
{
    internal class SetupCommand
    {
        private static DiscordSocketClient? Client;

        public SetupCommand(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;
            Client = client;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (Client == null || !command.Data.Name.Equals("setup")) return;

            await command.DeferAsync();

            ulong? guildId = command.GuildId;

            EmbedBuilder embed = new EmbedBuilder()
                .WithCurrentTimestamp();

            if (Client == null || guildId == null)
            {
                await command.RespondAsync(embed: embed.WithColor(Color.Red).Build());
                return;
            }

            switch (command.Data.Options.First().Value)
            {
                case "setup-ai-menu":
                    embed.Title = "Inteligência Artificial";
                    embed.Description = "Inicie uma conversa com um modelo A.I";
                    embed.ThumbnailUrl = "https://imgur.com/tnh71Er.gif";

                    embed.AddField("🤔 Como Funciona", "Escolha um modelo e um canal privado será criado.");
                    embed.AddField("⚙️ Capacidade", "Atualmente o sistema é capaz de gerar conversas e imagens.");
                    embed.AddField("♾️ Versão", "Projeto em fase beta! apresentou alguns erros e bugs? Por favor, reporte-os!");

                    ComponentBuilder builder = new ComponentBuilder();

                    foreach (ModelCategory category in Enum.GetValues(typeof(ModelCategory)))
                    {
                        string name = category.ToString().ToLower();

                        SelectMenuBuilder menu = new SelectMenuBuilder()
                        .WithPlaceholder(name)
                        .WithCustomId($"{name}-chat-menu");

                        foreach (ChatModel model in AiManager.Models)
                        {
                            if (model.Category != category) continue;

                            string description = model.Type switch
                            {
                                ModelType.Chat => "Chat",
                                ModelType.Question => "Questão",
                                ModelType.Image => "Imagem",
                                _ => "Desconhecido"
                            };

                            menu.AddOption(new SelectMenuOptionBuilder
                            {
                                Label = model.DisplayName,
                                Value = model.Model,
                                Description = description
                            });
                        }

                        builder.WithSelectMenu(menu);
                    }

                    await command.FollowupAsync(embed: embed.Build(), components: builder.Build());
                    break;
            }
        }
    }
}