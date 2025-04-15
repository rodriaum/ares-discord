/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Discord.Util;
using Ares.Core;
using Ares.Core.Manager;
using Ares.Core.Objects.Model;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Commands;

internal class SetupCommand
{
    private static DiscordSocketClient? _client;

    public SetupCommand(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += SlashCommandHandler;
        _client = client;
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (_client == null || !command.Data.Name.Equals("setup")) return;

        ulong? guildId = command.GuildId;

        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription(AresConstant.LoadingEmote)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embed.Build());
        RestInteractionMessage message = await command.GetOriginalResponseAsync();

        if (guildId is null)
        {
            await message.ModifyAsync(it => it.Embed = embed.WithDescription("Não foi possível encontrar a guilda atual.").Build());
            return;
        }

        if (command.Data.Options.Count == 0)
        {
            await message.ModifyAsync(it => it.Embed = embed.WithDescription("Nenhuma opção fornecida.").Build());
            return;
        }

        switch (command.Data.Options.First().Value)
        {
            case "setup-ai-menu":
                embed.Title = "Inteligência Artificial";
                embed.Description = "Inicie uma conversa com um modelo AI";
                embed.ThumbnailUrl = "https://imgur.com/tnh71Er.gif";

                embed.AddField("🤔 Como Funciona", "Escolha um modelo e um canal privado será criado.");
                embed.AddField("⚙️ Capacidade", "Atualmente o sistema é capaz de gerar conversas e imagens.");
                embed.AddField("♾️ Versão", "Projeto em fase beta! apresentou alguns erros e bugs? Por favor, reporte-os!");

                ComponentBuilder builder = new ComponentBuilder();

                if (AiManager.Models == null || !AiManager.Models.Any())
                {
                    await message.ModifyAsync(it => it.Embed = embed.WithDescription("Nenhum modelo de IA disponível.").Build());
                    return;
                }

                foreach (ModelCategory category in Enum.GetValues(typeof(ModelCategory)))
                {
                    string name = category.ToString();

                    SelectMenuBuilder menu = new SelectMenuBuilder()
                        .WithPlaceholder(name)
                        .WithCustomId($"chat-menu-{name.ToLower()}");

                    foreach (ChatModel model in AiManager.Models)
                    {
                        if (model.Category != category) continue;

                        var modelText = model.Type switch
                        {
                            ModelType.Chat => "Chat",
                            ModelType.Question => "Questão",
                            ModelType.Image => "Imagem",
                            ModelType.TTS => "Audio",
                            ModelType.Vision => "Visão",
                            _ => "Desconhecido"
                        };

                        string availableText = (model.Dev ? "Desenvolvimento" : (model.Exclusive ? "Exclusivo" : (model.Available ? "Disponível" : "Indisponível")));

                        menu.AddOption(new SelectMenuOptionBuilder
                        {
                            Label = model.DisplayName,
                            Value = model.Model,
                            Description = $"{modelText} ({model.RequestType.ToString()}): {availableText}",
                            Emote = AresUtil.GetEmojiByModelType(model.Type)
                        });
                    }

                    if (menu.Options.Count >= 1)
                    {
                        builder.WithSelectMenu(menu);
                    }
                }

                await message.ModifyAsync(it =>
                {
                    it.Embed = embed.Build();
                    it.Components = builder.Build();
                });
                break;
        }
    }
}