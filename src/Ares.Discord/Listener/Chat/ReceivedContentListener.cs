/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Database;
using Ares.Core.Models.Database.Chat.Sub;
using Ares.Discord.Util;
using Ares.Core;
using Ares.Core.Database.Collection;
using Ares.Core.Objects.Chat;
using Ares.Core.Objects.Chat.Image;
using Ares.Core.Objects.Chat.Price;
using Ares.Core.Objects.Language;
using Ares.Core.Objects.Model;
using Ares.Core.Service;
using Ares.Core.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Ares.Discord.Listener.Chat;

internal class ReceivedContentListener
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
    private async Task MessageReceivedHandler(SocketMessage args)
    {
        try
        {
            if (args is not SocketUserMessage message)
                return;

            if (!(args.Channel is SocketTextChannel channel))
                return;

            IUser user = args.Author;
            if (user.Id.Equals(_client.CurrentUser.Id))
                return;

            SocketGuild socketGuild = channel.Guild;
            if (socketGuild == null)
            {
                await channel.SendMessageAsync(AresConstant.UnableGetMember);
                return;
            }

            GuildCollection? data = AresCore.GuildCollection;
            if (data == null)
            {
                await channel.SendMessageAsync(AresConstant.UnablePerformTask);
                return;
            }

            Guild? guild = await data.FetchAsync(socketGuild.Id);
            if (guild == null)
                return;

            // Check if the channel is in the correct category and the user has an active conversation
            // Alert: This code must be right here, if you move it to another place there may be problems.
            if (!(channel.CategoryId.Equals(guild.Config?.ChatsCategoryId) &&
                  guild.HasActiveUserConversation(user, channel: channel.Id)))
                return;

            // Check if the channel belongs to the user
            if (!channel.Name.Contains(user.GlobalName.ToLower()))
                return;

            // Create initial embed
            EmbedBuilder embed = CreateInitialEmbed(guild);
            RestUserMessage botMessage = await channel.SendMessageAsync(embed: embed.Build());

            // Search for necessary information
            GChatInfoModel? info = guild.ChatInfoByChannel(user, channel.Id);
            if (info == null)
            {
                await ModifyMessageWithError(botMessage, embed, guild.GetTranslation(LangKeys.CouldNotFindInfo));
                return;
            }

            ChatModel? model = guild.GetLastModelByUser(user, channel: channel.Id);
            if (model == null)
            {
                await ModifyMessageWithError(botMessage, embed, guild.GetTranslation(LangKeys.CouldNotFindLastModel));
                return;
            }

            string prompt = message.Content;

            // Future: Compatibility with files, etc.
            if (string.IsNullOrWhiteSpace(prompt))
            {
                await ModifyMessageWithError(botMessage, embed, "Atualmente o sistema de chat só está disponível por mensagem.");
                return;
            }

            SocketGuildUser guildUser = socketGuild.GetUser(user.Id);
            List<GChatHistoricModel>? historics = guild.ChatHistoricsByChannel(user, channel.Id);

            // Process message based on template type
            switch (model.Type)
            {
                case ModelType.Chat:
                    await ProcessChatModel(guild, guildUser, model, channel.Id, prompt, botMessage, embed, historics);
                    break;

                case ModelType.Image:
                    await ProcessImageModel(guild, guildUser, model, info, channel.Id, prompt, botMessage, embed, historics);
                    break;

                case ModelType.TTS:
                    await ProcessTTSModel(guild, guildUser, model, channel.Id, prompt, botMessage, embed, historics);
                    break;

                default:
                    await UpdateBotMessage
                        (
                            botMessage,
                            new EmbedBuilder()
                                .WithDescription(guild.GetTranslation(LangKeys.CouldNotFindModel))
                                .WithColor(Color.Red)
                        );
                    break;
            }
        }
        catch (Exception e)
        {
            await AresLogger.ErrorAsync(nameof(MessageReceivedHandler), "Can't process the content receiver.", e.Message);
        }
    }

    private EmbedBuilder CreateInitialEmbed(Guild guild)
    {
        return new EmbedBuilder()
            .WithTitle(guild.GetTranslation(LangKeys.AI))
            .WithDescription(guild.GetTranslation(LangKeys.ToProcess))
            .WithColor(Color.Gold)
            .WithFooter(guild.GetTranslation(LangKeys.TakeUpMinutes));
    }

    private async Task ModifyMessageWithError(RestUserMessage botMessage, EmbedBuilder embed, string errorMessage)
    {
        await botMessage.ModifyAsync(message =>
            message.Embeds = new[] { embed.WithDescription(errorMessage).Build() });
    }

    private async Task ProcessChatModel(
        Guild guild,
        SocketGuildUser user,
        ChatModel model,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        List<GChatHistoricModel>? historics)
    {
        DateTime date = DateTime.Now;
        EmbedBuilder? priceEmbed = null;

        var (responseText, success) = await AiService.GenerateConversationAsync(guild, user, model, channelId, prompt, botMessage);

        if (success)
        {
            // Set color based on model category
            Color color = AresUtil.GetColorByModelCategory(model.Category);

            // Handle Discord's description character limit
            if (responseText.Length > 4096)
            {
                embed.WithDescription(responseText.Substring(0, 4096))
                    .WithColor(color)
                    .WithFooter($"{date.Year} - Ares | {model.DisplayName} (♾️)");
            }
            else
            {
                embed.WithDescription(responseText)
                    .WithColor(color)
                    .WithFooter($"{date.Year} - Ares | {model.DisplayName}");
            }

            // Atualiza o historico de chat após a geracão.
            historics = guild.ChatHistorics(user, channel: channelId);

            // Process pricing information
            priceEmbed = CreatePriceEmbedForChat(guild, model, historics);
        }
        else
        {
            embed.WithDescription(responseText)
                .WithColor(Color.Red)
                .WithFooter($"{date.Year} - Ares | {model.DisplayName}");
        }

        await UpdateBotMessage(botMessage, embed, priceEmbed);
    }

    private async Task ProcessImageModel(
        Guild guild,
        SocketGuildUser guildUser,
        ChatModel model,
        GChatInfoModel info,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        List<GChatHistoricModel>? historics)
    {
        ImageGenOptions options = info.ImageGenOptions ?? new ImageGenOptions();

        var (responseImageUrl, success) = await AiService.GenerateImageUrlAsync(guild, guildUser, model, options, channelId, prompt);

        if (success)
        {
            // Check if the result is a valid URL
            if (WebUtil.IsValidUrl(responseImageUrl))
            {
                embed.WithDescription(guild.GetTranslation(LangKeys.Success))
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
            embed.WithDescription(guild.GetTranslation(LangKeys.UnableGenerateOrder))
                .WithColor(Color.Red);
        }

        // Process pricing information
        EmbedBuilder? priceEmbed = CreatePriceEmbedForImage(guild, model, historics, options);
        await UpdateBotMessage(botMessage, embed, priceEmbed);
    }

    private async Task ProcessTTSModel(
        Guild guild,
        SocketGuildUser guildUser,
        ChatModel model,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        List<GChatHistoricModel>? historics)
    {
        (string responseBinary, bool isAudio) = await AiService.GenerateTTSAsync(guild, guildUser, model, channelId, prompt);

        // Set color based on model category
        Color color = AresUtil.GetColorByModelCategory(model.Category);
        DateTime date = DateTime.Now;

        Optional<IEnumerable<FileAttachment>>? attachments = null;

        // Handle Discord's description character limit
        if (string.IsNullOrEmpty(responseBinary) || !isAudio)
        {
            embed.WithDescription(guild.GetTranslation(LangKeys.UnableGenerateOrder))
                .WithFooter($"{date.Year} - Ares | {model.DisplayName}");
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
                    .WithFooter($"{date.Year} - Ares | {model.DisplayName}");

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
                embed.WithDescription(guild.GetTranslation(LangKeys.UnableGenerateOrder))
                    .WithFooter($"{date.Year} - Ares | {model.DisplayName}");

                AresLogger.Error("TTS", "Unable to generate TTS audio.", ex.Message);
            }
        }

        // Process pricing information
        // EmbedBuilder? priceEmbed = CreatePriceEmbedForChat(guild, model, historics);
        await UpdateBotMessage(botMessage, embed, /*priceEmbed,*/ attachments: attachments);
    }

    private EmbedBuilder? CreatePriceEmbedForChat(Guild guild, ChatModel model, List<GChatHistoricModel>? historics)
    {
        if (historics == null || !historics.Any())
            return null;

        GChatHistoricModel? historic = historics.LastOrDefault();
        if (historic == null)
            return null;

        ChatValueUsage? usage = historic.Usage;
        ChatPriceUsage? price = model.Price;

        if (usage == null || price == null)
            return null;

        decimal inputPrice = usage.InputTokens * price.InputPricePerToken;
        decimal outputPrice = usage.OutputTokens * price.OutputPricePerToken;

        EmbedBuilder priceEmbed = new EmbedBuilder()
            // Input Field
            .AddField("Tokens", usage.InputTokens, true)
            .AddField(guild.GetTranslation(LangKeys.Request), $"$ {FormatterUtil.FormatPrice(inputPrice)}", true)
            // Broke Line
            .AddField("\u200B", "\u200B", false)
            // Output Field
            .AddField("Tokens", usage.OutputTokens, true)
            .AddField(guild.GetTranslation(LangKeys.Response), $"$ {FormatterUtil.FormatPrice(outputPrice)}", true)
            // Broke Line
            .AddField("\u200B", "\u200B", false)
            // Total Field
            .AddField("Tokens", usage.TotalTokens(), true)
            .AddField(guild.GetTranslation(LangKeys.Total), $"$ {FormatterUtil.FormatPrice(inputPrice + outputPrice)}", true)
            .WithFooter(guild.GetTranslation(LangKeys.PriceLowerCache));

        return priceEmbed;
    }

    private EmbedBuilder? CreatePriceEmbedForImage(Guild guild, ChatModel model, List<GChatHistoricModel>? historics, ImageGenOptions options)
    {
        if (historics == null || !historics.Any())
            return null;

        GChatHistoricModel? historic = historics.LastOrDefault();
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
            .AddField(guild.GetTranslation(LangKeys.Total), $"$ {FormatterUtil.FormatPrice(priceDetail.Price)}", true)
            .WithFooter("Ares");

        return priceEmbed;
    }

    private async Task UpdateBotMessage(RestUserMessage botMessage, EmbedBuilder mainEmbed, EmbedBuilder? priceEmbed = null, Optional<IEnumerable<FileAttachment>>? attachments = null)
    {
        List<Embed> embeds = new List<Embed>();

        if (priceEmbed != null)
        {
            embeds.Add(priceEmbed.Build());
        }

        // Alert: Add the main embed at the end so it is the last one displayed
        embeds.Add(mainEmbed.Build());

        // Fix in case someone deletes the channel before sending the message.
        if (botMessage == null) return;

        if (attachments != null)
        {
            var attachment = attachments.Value;

            await botMessage.ModifyAsync(message =>
            {
                message.Embeds = embeds.ToArray();
                message.Attachments = attachment;
            });
        }
        else
        {
            await botMessage.ModifyAsync(message => message.Embeds = embeds.ToArray());
        }
    }
}