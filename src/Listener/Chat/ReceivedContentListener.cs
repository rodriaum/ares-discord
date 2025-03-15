using Ares.src.Database.Collection;
using Ares.src.Database.Model;
using Ares.src.Database.Model.Chat.Sub;
using Ares.src.Objects.Chat;
using Ares.src.Objects.Chat.Image;
using Ares.src.Objects.Chat.Price;
using Ares.src.Objects.Language;
using Ares.src.Objects.Model;
using Ares.src.Service;
using Ares.src.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MongoDB.Bson;

namespace Ares.src.Listener.Chat;

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
                await channel.SendMessageAsync(Constant.UNABLE_GET_MEMBER);
                return;
            }

            GuildCollection? data = Program.GuildCollection;
            if (data == null)
            {
                await channel.SendMessageAsync(Constant.UNABLE_PERFORM_TASK);
                return;
            }

            Guild? guild = await data.Fetch(socketGuild.Id);
            if (guild == null)
                return;

            // Check if the channel is in the correct category and the user has an active conversation
            // Alert: This code must be right here, if you move it to another place there may be problems.
            if (!(channel.CategoryId.Equals(guild.Information?.Config?.ChatsCategoryId) &&
                  guild.HasActiveUserConversation(user)))
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

            // Process message based on template type
            SocketGuildUser guildUser = socketGuild.GetUser(user.Id);
            string prompt = message.Content;
            List<GChatHistoricModel>? historics = guild.ChatHistoricsByChannel(user, channel.Id);

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
            await LogUtil.ErrorAsync(nameof(MessageReceivedHandler), "Can't process the content receiver.", e.Message);
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
        SocketGuildUser guildUser,
        ChatModel model,
        ulong channelId,
        string prompt,
        RestUserMessage botMessage,
        EmbedBuilder embed,
        List<GChatHistoricModel>? historics)
    {
        string responseText = await AiService.GenerateConversationAsync(guild, guildUser, model, channelId, prompt, botMessage);

        // Set color based on model category
        Color color = GetColorForModelCategory(model.Category);
        DateTime date = DateTime.Now;

        // Handle Discord's description character limit
        if (responseText.Length > 4096)
        {
            embed.WithDescription(responseText.Substring(0, 4096))
                .WithColor(color)
                .WithFooter($"{date.Year} - Ares | {model.DisplayName} (Limite de caracteres alcançado)");
        }
        else
        {
            embed.WithDescription(responseText)
                .WithColor(color)
                .WithFooter($"{date.Year} - Ares | {model.DisplayName}");
        }

        // Process pricing information
        EmbedBuilder? priceEmbed = CreatePriceEmbedForChat(guild, model, historics);
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
        ImageGenOptions options = info.ImageGenOptions ??
                                 new ImageGenOptions(ImageQuality.Standard, ImageSize.W1024xH1024, ImageStyle.Natural);

        string responseImageUrl = await AiService.GenerateImageUrlAsync(guild, guildUser, model, options, channelId, prompt);

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
        Color color = GetColorForModelCategory(model.Category);
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

                LogUtil.Log("Audio", $"Audio file size: {fileSize} bytes");

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

                LogUtil.Error("TTS", "Unable to generate TTS audio.", ex.Message);
            }
        }

        // Process pricing information
        // EmbedBuilder? priceEmbed = CreatePriceEmbedForChat(guild, model, historics);
        await UpdateBotMessage(botMessage, embed, /*priceEmbed,*/ attachments: attachments);
    }

    private Color GetColorForModelCategory(ModelCategory category)
    {
        return category switch
        {
            ModelCategory.OpenAI => Color.Green,
            ModelCategory.Anthropic => Color.Orange,
            ModelCategory.DeepSeek => Color.Blue,
            _ => Color.Default
        };
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