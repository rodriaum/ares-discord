/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Constants;
using Ares.Common.Models.Data;
using Ares.Common.Models.Data.Chat.Model;
using Ares.Common.Objects;
using Ares.Common.Util;
using Ares.Discord.Service.Neural;
using Ares.Discord.Services.Api;
using Ares.Discord.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace Ares.Discord.Commands;

public class SetupCommand
{
    private static DiscordSocketClient? _client;

    private static ChatModelService? _chatModelService { get; set; }

    public SetupCommand(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += SlashCommandHandler;
        _client = client;

        _chatModelService = Program.ChatModelService;

        if (_chatModelService == null)
        {
            AresLogger.Log(nameof(NeuralService), "ChatModel service is not initialized.", severity: Severity.Error);
            throw new InvalidOperationException("ChatModel service is not initialized.");
        }
    }

    private Task SlashCommandHandler(SocketSlashCommand command)
    {
        _ = Task.Run(async () =>
        {
            if (_client == null || !command.Data.Name.Equals("setup")) return;

            ulong? guildId = command.GuildId;

            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription(AppConstants.LoadingEmote)
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
                    await handleModelsMessage(message, embed);
                    break;

                case "setup-ai-web-menu":
                    await handleModelsMessage(message, embed, ChatRequestType.Web);
                    break;

                case "setup-ai-local-menu":
                    await handleModelsMessage(message, embed, ChatRequestType.Local);
                    break;
            }
        });

        return Task.CompletedTask;
    }

    private async static Task handleModelsMessage(RestInteractionMessage message, EmbedBuilder embed, ChatRequestType? requestType = null)
    {
        embed.Title = "Inteligência Artificial";
        embed.Description = "Inicie uma conversa com um modelo AI";
        embed.ThumbnailUrl = "https://imgur.com/tnh71Er.gif";

        embed.AddField("🤔 Como Funciona", "Escolha um modelo e um canal privado será criado.");
        embed.AddField("⚙️ Capacidade", "Atualmente o sistema é capaz de gerar conversas, imagens e áudios.");
        embed.AddField("🛠️ Desenvolvimento", "O sistema está em desenvolvimento e pode apresentar erros ou bugs. Se você encontrar algum, por favor, reporte-o.");

        if (requestType != null)
            embed.WithFooter($"Modelos {requestType}");

        ConcurrentBag<ChatModel>? models = await _chatModelService!.GetAllModels();

        if (models == null || !models.Any())
        {
            await message.ModifyAsync(it => it.Embed = embed.WithDescription("Nenhum modelo de IA disponível.").Build());
            return;
        }

        // Cria uma lista para armazenar os menus
        List<SelectMenuBuilder> menus = new List<SelectMenuBuilder>();

        foreach (ModelCategory category in Enum.GetValues(typeof(ModelCategory)))
        {
            string name = category.ToString();

            SelectMenuBuilder menu = new SelectMenuBuilder()
                .WithPlaceholder(name)
                .WithCustomId($"chat-menu-{name.ToLower()}");

            foreach (ChatModel model in models)
            {
                if ((requestType != null && model.RequestType != requestType) || model.Category != category) continue;

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
                    Value = model.Id,
                    Description = $"{modelText} ({model.RequestType.ToString()}): {availableText}",
                    Emote = AresUtil.GetEmojiByModelType(model.Type)
                });
            }

            if (menu.Options.Count >= 1)
            {
                menus.Add(menu);
            }
        }

        const int maxMenusPerMessage = 5;
        int totalMessages = (int)Math.Ceiling((double)menus.Count / maxMenusPerMessage);

        ComponentBuilder firstBuilder = new ComponentBuilder();
        int menusToAdd = Math.Min(maxMenusPerMessage, menus.Count);

        for (int i = 0; i < menusToAdd; i++)
        {
            firstBuilder.WithSelectMenu(menus[i]);
        }

        await message.ModifyAsync(it =>
        {
            it.Embed = embed.Build();
            it.Components = firstBuilder.Build();
        });

        for (int msgIndex = 1; msgIndex < totalMessages; msgIndex++)
        {
            ComponentBuilder additionalBuilder = new ComponentBuilder();

            int startIndex = msgIndex * maxMenusPerMessage;
            int count = Math.Min(maxMenusPerMessage, menus.Count - startIndex);

            for (int i = 0; i < count; i++)
            {
                additionalBuilder.WithSelectMenu(menus[startIndex + i]);
            }

            await message.Channel.SendMessageAsync(
                text: msgIndex == 1 ? " " : "",
                components: additionalBuilder.Build());
        }
    }
}