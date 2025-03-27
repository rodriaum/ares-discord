using Discord;
using Discord.WebSocket;
using Ares.Core.Manager;
using Ares.Core.Objects.Model;
using Ares.Core.Util;

namespace Ares.Discord.Commands;

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

        if (guildId is null)
        {
            await command.RespondAsync(embed: embed.WithColor(Color.Red).Build());
            return;
        }

        if (command.Data.Options.Count == 0)
        {
            await command.RespondAsync("Nenhuma opção fornecida.", ephemeral: true);
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

                if (AiManager.Models == null || !AiManager.Models.Any())
                {
                    await command.RespondAsync("Nenhum modelo de IA disponível.", ephemeral: true);
                    return;
                }

                foreach (ModelCategory category in Enum.GetValues(typeof(ModelCategory)))
                {
                    string name = category.ToString();

                    SelectMenuBuilder menu = new SelectMenuBuilder()
                        .WithPlaceholder(FormatterUtil.CapitalizeFirstLetter(name))
                        .WithCustomId($"chat-menu-{name}");

                    foreach (ChatModel model in AiManager.Models)
                    {
                        if (model.Category != category) continue;

                        string modelText = model.Type switch
                        {
                            ModelType.Chat => "Chat",
                            ModelType.Question => "Questão",
                            ModelType.Image => "Imagem",
                            ModelType.TTS => "Audio",
                            _ => "Desconhecido"
                        };

                        string availableText = (model.Exclusive ? "Exclusivo" : (model.Available ? "Disponível" : "Indisponível"));

                        menu.AddOption(new SelectMenuOptionBuilder
                        {
                            Label = model.DisplayName,
                            Value = model.Model,
                            Description = $"{modelText}: {availableText}"
                        });
                    }

                    if (menu.Options.Count > 1)
                    {
                        builder.WithSelectMenu(menu);
                    }
                }

                await command.FollowupAsync(embed: embed.Build(), components: builder.Build());
                break;
        }
    }
}