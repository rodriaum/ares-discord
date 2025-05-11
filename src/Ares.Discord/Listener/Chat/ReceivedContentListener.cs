/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Constants;
using Ares.Core.Manager.Database;
using Ares.Core.Models.Chat.Historic;
using Ares.Core.Models.Collection;
using Ares.Core.Objects;
using Ares.Core.Objects.Chat;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Chat.Price;
using Ares.Core.Objects.Model;
using Ares.Core.Repository;
using Ares.Core.Service;
using Ares.Core.Util;
using Ares.Discord.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace Ares.Discord.Listener.Chat;

public class ReceivedContentListener
{
    private readonly DiscordSocketClient _client;

    /// <summary>
    /// Constructor that initializes the Received Intent Listener with a Discord client.
    /// </summary>
    /// <param name="client">The Discord client</param>
    public ReceivedContentListener(DiscordSocketClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _client.MessageReceived += MessageReceivedHandler;
    }

    /// <summary>
    /// Handler for incoming messages.
    /// </summary>
    private Task MessageReceivedHandler(SocketMessage args)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (args is not SocketUserMessage message)
                    return;

                if (!(args.Channel is SocketTextChannel channel))
                    return;

                IUser iuser = args.Author;
                if (iuser.Id.Equals(_client.CurrentUser.Id))
                    return;

                SocketGuild socketGuild = channel.Guild;
                if (socketGuild == null)
                {
                    await channel.SendMessageAsync(AresConstant.UnableGetMember);
                    return;
                }

                GuildRepository? guildRepository = AresCore.GuildRepository;
                UserRepository? userRepository = AresCore.UserRepository;

                if (guildRepository == null || userRepository == null)
                {
                    await channel.SendMessageAsync(AresConstant.UnablePerformTask);
                    return;
                }

                Guild? guild = await guildRepository.FetchAsync(socketGuild.Id);
                if (guild == null)
                    return;

                User? user = await userRepository.FetchAsync(iuser.Id, saveInRedis: true);
                if (user == null)
                    return;

                // Check if the channel is in the correct category and the user has an active conversation
                // Alert: This code must be right here, if you move it to another place there may be problems.
                if (!(channel.CategoryId.Equals(guild.Preferences?.ChatsCategoryId) &&
                      UserManager.HasActiveUserConversation(user, guild.Id, channelId: channel.Id)))
                    return;

                // Check if the channel belongs to the user
                if (!channel.Name.Contains(iuser.GlobalName.ToLower()))
                    return;

                // Create initial embed
                EmbedBuilder embed = CreateInitialEmbed(guild);
                RestUserMessage botMessage = await channel.SendMessageAsync(embed: embed.Build());

                // Search for necessary information
                UserChatInfo? info = UserManager.ChatInfoByChannel(user, guild.Id, channel.Id);
                if (info == null)
                {
                    await ModifyMessageWithError(botMessage, embed, GuildManager.GetTranslation(guild, LangKeysConstant.CouldNotFindInfo));
                    return;
                }

                ChatModel? model = await UserManager.GetLastModelByUser(user, guild.Id, channel: channel.Id);
                if (model == null)
                {
                    await ModifyMessageWithError(botMessage, embed, GuildManager.GetTranslation(guild, LangKeysConstant.CouldNotFindLastModel));
                    return;
                }

                string prompt = message.Content;

                // Future: Compatibility with files, etc.
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    await ModifyMessageWithError(
                            botMessage,
                            embed.WithColor(Color.DarkOrange).WithFooter(AresConstant.AppName),
                            "Atualmente o sistema de chat só está disponível por mensagem."
                        );
                    return;
                }

                SocketGuildUser guildUser = socketGuild.GetUser(iuser.Id);
                List<UserChatHistoric>? historics = UserManager.ChatHistoricsByChannel(user, guild.Id, channel.Id);

                // Process message based on template type
                switch (model.Type)
                {
                    case ModelType.Chat:
                        await ProcessChatModel(guild, user, model, channel.Id, prompt, botMessage, embed, userRepository, historics);
                        break;

                    case ModelType.Image:
                        await ProcessImageModel(guild, user, model, info, channel.Id, prompt, botMessage, embed, historics);
                        break;

                    case ModelType.TTS:
                        await ProcessTTSModel(guild, user, model, channel.Id, prompt, botMessage, embed, historics);
                        break;

                    default:
                        await UpdateBotMessage
                            (
                                botMessage,
                                new EmbedBuilder()
                                    .WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.CouldNotFindModel))
                                    .WithColor(Color.Red)
                            );
                        break;
                }
            }
            catch (Exception e)
            {
                await AresLogger.LogAsync(nameof(MessageReceivedHandler), "Can't process the content receiver.", e.Message, severity: Severity.Error);
            }
        });

        return Task.CompletedTask;
    }

    private EmbedBuilder CreateInitialEmbed(Guild guild)
    {
        return new EmbedBuilder()
            .WithTitle(GuildManager.GetTranslation(guild, LangKeysConstant.AI))
            .WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.ToProcess))
            .WithColor(Color.Gold)
            .WithFooter(GuildManager.GetTranslation(guild, LangKeysConstant.TakeUpMinutes));
    }

    private async Task ModifyMessageWithError(RestUserMessage botMessage, EmbedBuilder embed, string errorMessage)
    {
        await botMessage.ModifyAsync(message =>
            message.Embeds = new[] { embed.WithDescription(errorMessage).Build() });
    }

    private async Task ProcessChatModel(
        Guild guild,
        User user,
        ChatModel model,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        UserRepository userRepository,
        List<UserChatHistoric>? historics)
    {
        DateTime date = DateTime.Now;

        EmbedBuilder? priceEmbed = null;

        string menuId = $"chat-snippet-{StringUtil.GenerateExclusiveCode()}";

        SelectMenuBuilder menu = new SelectMenuBuilder()
            .WithPlaceholder("Lista de Trechos")
            .WithCustomId(menuId);

        var (result, success) = await NeuralService.GenerateConversationAsync(guild, user, model, channelId, prompt);

        if (success)
        {
            MatchCollection matches = Regex.Matches(result, "```(?:[a-zA-Z0-9]*\\n)?(.*?)```", RegexOptions.Singleline);

            uint index = 0;

            List<UserChatSnippet> snippets = new();

            foreach (Match match in matches)
            {
                string code = match.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(code)) continue;

                UserChatSnippet snippet = new UserChatSnippet(channelId, botMessage.Id, index, code);

                snippets.Add(snippet);

                menu.AddOption(new SelectMenuOptionBuilder
                {
                    Label = $"Trecho n.º {index + 1}",
                    Value = snippet.Id,
                });

                index++;
            }

            await UserManager.SaveSnippetsAsync(user, guild.Id, snippets);

            // Set color based on model category
            Color color = AresUtil.GetColorByModelCategory(model.Category);

            const int Limit = 4096;

            // Handle Discord's description character limit
            if (result.Length > Limit)
            {
                embed.WithDescription(result.Substring(0, Limit))
                    .WithColor(color)
                    .WithFooter($"{date.Year} - {AresConstant.AppName} | {model.DisplayName} (♾️)");
            }
            else
            {
                embed.WithDescription(result)
                    .WithColor(color)
                    .WithFooter($"{date.Year} - {AresConstant.AppName} | {model.DisplayName}");
            }

            // Atualiza o historico de chat após a geracão.
            historics = UserManager.ChatHistorics(user, guild.Id, channelId: channelId);

            // Process pricing information
            priceEmbed = CreatePriceEmbedForChat(guild, model, historics);
        }
        else
        {
            embed.WithDescription(result)
                .WithColor(Color.Red)
                .WithFooter($"{date.Year} - {AresConstant.AppName} | {model.DisplayName}");
        }

        await UpdateBotMessage(botMessage, embed, priceEmbed: priceEmbed, selectMenu: menu);
    }

    private async Task ProcessImageModel(
        Guild guild,
        User user,
        ChatModel model,
        UserChatInfo info,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        List<UserChatHistoric>? historics)
    {
        ImageGenOptions options = info.ImageGenOptions ?? new ImageGenOptions();

        var (responseImageUrl, success) = await NeuralService.GenerateImageUrlAsync(guild, user, model, options, channelId, prompt);

        if (success)
        {
            // Check if the result is a valid URL
            if (WebUtil.IsValidUrl(responseImageUrl))
            {
                embed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.Success))
                    .WithColor(Color.Green)
                    .WithImageUrl(responseImageUrl);
            }
            else
            {
                embed.WithDescription(responseImageUrl)
                    .WithColor(Color.Red);
            }
        }
        else
        {
            embed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.UnableGenerateOrder))
                .WithColor(Color.Red);
        }

        // Process pricing information
        EmbedBuilder? priceEmbed = CreatePriceEmbedForImage(guild, model, historics, options);
        await UpdateBotMessage(botMessage, embed, priceEmbed);
    }

    private async Task ProcessTTSModel(
        Guild guild,
        User user,
        ChatModel model,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        List<UserChatHistoric>? historics)
    {
        (string responseBinary, bool isAudio) = await NeuralService.GenerateTTSAsync(guild, user, model, channelId, prompt);

        // Set color based on model category
        Color color = AresUtil.GetColorByModelCategory(model.Category);
        DateTime date = DateTime.Now;

        Optional<IEnumerable<FileAttachment>>? attachments = null;

        // Handle Discord's description character limit
        if (string.IsNullOrEmpty(responseBinary) || !isAudio)
        {
            embed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.UnableGenerateOrder))
                .WithFooter($"{date.Year} - {AresConstant.AppName} | {model.DisplayName}");
        }
        else
        {
            try
            {
                BinaryData binaryData = BinaryData.FromString(responseBinary);
                MemoryStream memory = new MemoryStream();

                using (Stream stream = binaryData.ToStream())
                {
                    await stream.CopyToAsync(memory);
                }

                // Reposition MemoryStream to the beginning
                memory.Position = 0;

                embed.WithDescription($"🔊 TTS: {prompt}")
                    .WithFooter($"{date.Year} - {AresConstant.AppName} | {model.DisplayName}");

                long fileSize = memory.Length;

                AresLogger.Log("Audio", $"Audio file size: {fileSize} bytes");

                // If the file is very small (< 1KB), it is probably empty or corrupt.
                if (fileSize < 1024)
                {
                    embed.WithFooter("O arquivo de áudio é suspeitamente pequeno");
                }

                attachments = new Optional<IEnumerable<FileAttachment>>(
                    [
                    new FileAttachment(memory, "audio.mp3")
                    ]
                );
            }
            catch (Exception ex)
            {
                embed.WithDescription(GuildManager.GetTranslation(guild, LangKeysConstant.UnableGenerateOrder))
                    .WithFooter($"{date.Year} - {AresConstant.AppName} | {model.DisplayName}");

                AresLogger.Log("TTS", "Unable to generate TTS audio.", ex.Message, severity: Severity.Error);
            }
        }

        // Process pricing information
        // EmbedBuilder? priceEmbed = CreatePriceEmbedForChat(guild, model, historics);
        await UpdateBotMessage(botMessage, embed, /*priceEmbed,*/ attachments: attachments);
    }

    private EmbedBuilder? CreatePriceEmbedForChat(Guild guild, ChatModel model, List<UserChatHistoric>? historics)
    {
        if (historics == null || !historics.Any())
            return null;

        UserChatHistoric? historic = historics.LastOrDefault();
        if (historic == null)
            return null;

        ChatTokenUsage? usage = historic.Usage;
        ChatPriceUsage? price = model.Price;

        if (usage == null || price == null)
            return null;

        decimal inputPrice = usage.InputTokens * price.InputPriceTokenPerToken();
        decimal outputPrice = usage.OutputTokens * price.OutputPriceTokenPerToken();

        EmbedBuilder priceEmbed = new EmbedBuilder()
            // Input Field
            .AddField("Tokens", usage.InputTokens, true)
            .AddField(GuildManager.GetTranslation(guild, LangKeysConstant.Request), $"$ {FormatterUtil.FormatPrice(inputPrice)}", true)
            // Broke Line
            .AddField("\u200B", "\u200B", false)
            // Output Field
            .AddField("Tokens", usage.OutputTokens, true)
            .AddField(GuildManager.GetTranslation(guild, LangKeysConstant.Response), $"$ {FormatterUtil.FormatPrice(outputPrice)}", true)
            // Broke Line
            .AddField("\u200B", "\u200B", false)
            // Total Field
            .AddField("Tokens", usage.TotalTokens, true)
            .AddField(GuildManager.GetTranslation(guild, LangKeysConstant.Total), $"$ {FormatterUtil.FormatPrice(inputPrice + outputPrice)}", true)
            .WithFooter(GuildManager.GetTranslation(guild, LangKeysConstant.PriceLowerCache));

        return priceEmbed;
    }

    private EmbedBuilder? CreatePriceEmbedForImage(Guild guild, ChatModel model, List<UserChatHistoric>? historics, ImageGenOptions options)
    {
        if (historics == null || !historics.Any())
            return null;

        UserChatHistoric? historic = historics.LastOrDefault();
        if (historic == null)
            return null;

        ChatPriceUsage? price = model.Price;
        if (price == null)
            return null;

        List<ChatPriceUsageDetail>? priceDetails = price.ChatPriceUsageDetail;
        if (priceDetails == null)
            return null;

        ChatPriceUsageDetail? priceDetail = priceDetails.Find(x => x.Quality == options.Quality && x.Size == options.Size);
        if (priceDetail == null)
            return null;

        EmbedBuilder priceEmbed = new EmbedBuilder()
            .AddField(GuildManager.GetTranslation(guild, LangKeysConstant.Total), $"$ {FormatterUtil.FormatPrice(priceDetail.Price)}", true)
            .WithFooter(AresConstant.AppName);

        return priceEmbed;
    }

    private async Task UpdateBotMessage(
        RestUserMessage botMessage,
        EmbedBuilder mainEmbed,
        EmbedBuilder? priceEmbed = null,
        SelectMenuBuilder? selectMenu = null,
        Optional<IEnumerable<FileAttachment>>? attachments = null)
    {
        // Fix in case someone deletes the channel before sending the message.
        if (botMessage == null) return;

        List<Embed> embeds = new List<Embed>();

        if (priceEmbed != null)
        {
            embeds.Add(priceEmbed.Build());
        }

        // Alert: Add the main embed at the end so it is the last one displayed
        embeds.Add(mainEmbed.Build());

        ComponentBuilder componentBuilder = new ComponentBuilder();

        if (selectMenu != null && selectMenu.Options.Count > 0)
        {
            componentBuilder.WithSelectMenu(selectMenu);
        }

        MessageComponent component = componentBuilder.Build();

        if (attachments != null)
        {
            var attachment = attachments.Value;
            await botMessage.ModifyAsync(message =>
            {
                message.Embeds = embeds.ToArray();
                message.Attachments = attachment;
                message.Components = component;
            });
        }
        else
        {
            await botMessage.ModifyAsync(message =>
            {
                message.Embeds = embeds.ToArray();
                message.Components = component;
            });
        }
    }
}