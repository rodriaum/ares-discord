using Ares.src.Guild.Information;
using Ares.src.Utils.Extra;
using Ares.src.Utils;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using OpenAI.Images;
using Ares.src.Guild.Config;
using Ares.src.Service;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Objects.Chat;
using Ares.src.Objects.Model;
using Ares.src.Backend.Data;
using Ares.src.Objects.Language;

namespace Ares.src.Listener.Chat;

internal class ReceivedContentListener
{
    private static DiscordSocketClient? _client { get; set; }

    /// <summary>
    /// Construtor que inicializa o ReceivedContentListener com um cliente Discord.
    /// </summary>
    /// <param name="client">O cliente Discord.</param>
    public ReceivedContentListener(DiscordSocketClient client)
    {
        client.MessageReceived += MessageReceivedHandler;
        _client = client;
    }

    /// <summary>
    /// Manipulador para mensagens recebidas.
    /// </summary>
    /// <param name="args">A mensagem recebida.</param>
    private async Task MessageReceivedHandler(SocketMessage args)
    {
        try
        {
            SocketTextChannel? channel = args.Channel as SocketTextChannel;
            if (channel == null) return;

            IUser user = args.Author;

            if (_client == null || user == null)
            {
                await channel.SendMessageAsync(Constant.UNABLE_GET_MEMBER);
                return;
            }

            if (user.Id.Equals(_client.CurrentUser.Id)) return;
            if (args is not SocketUserMessage message) return;

            GuildData? data = Core.GuildData;

            if (data == null)
            {
                await channel.SendMessageAsync(Constant.UNABLE_PERFORM_TASK);
                return;
            }

            SocketGuild socketGuild = channel.Guild;

            if (socketGuild == null)
            {
                await channel.SendMessageAsync(Constant.UNABLE_GET_MEMBER);
                return;
            }

            Guild.Guild? guild = await data.Fetch(socketGuild.Id);
            if (guild == null) return;

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle(guild.GetTranslation(LangKeys.AI))
                .WithDescription(guild.GetTranslation(LangKeys.ToProcess))
                .WithColor(Color.Gold)
                .WithFooter(guild.GetTranslation(LangKeys.TakeUpMinutes));

            GuildInformation information = guild.Information;

            if (information == null)
            {
                await channel.SendMessageAsync(embed: embed.WithDescription(guild.GetTranslation(LangKeys.CouldNotFindInfo)).Build());
                return;
            }

            GuildConfigData? gcd = information.Config;

            if (gcd == null)
            {
                await channel.SendMessageAsync(embed: embed.WithDescription(guild.GetTranslation(LangKeys.CouldNotFindInfoID)).Build());
                return;
            }

            if (!(channel.CategoryId.Equals(gcd.ChatsCategoryId) && guild.HasActiveUserConversation(user))) return;

            // O método só é ivocado aqui porque ele iria enviar mensagem sem a verificação de cima estar finalizada.
            RestUserMessage botMessage = await channel.SendMessageAsync(embed: embed.Build());

            // Verifica se o canal em que o usuário enviou a mensagem é dele. (Futuramente pode ser verificado com banco de dados)
            if (!channel.Name.Contains(user.GlobalName.ToLower())) return;

            // IRole exclusiveRole = socketGuild.GetRole(gcd.ExclusiveRoleId);

            ChatModel? model = guild.GetLastModelByUser(user, channel: channel.Id);

            if (model == null)
            {
                await channel.SendMessageAsync(embed: embed.WithDescription(guild.GetTranslation(LangKeys.CouldNotFindLastModel)).Build());
                return;
            }

            SocketGuildUser guildUser = socketGuild.GetUser(user.Id);
            string prompt = message.Content;

            EmbedBuilder? priceEmbed = null;

            switch (model.Type)
            {
                case ModelType.Chat:
                    string responseText = await AiService.GenerateConversationAsync(guild, guildUser, model, channel.Id, prompt, botMessage);

                    Color color = model.Category switch
                    {
                        ModelCategory.OpenAI => Color.Green,
                        ModelCategory.Anthropic => Color.Orange,
                        ModelCategory.DeepSeek => Color.Blue,
                        _ => Color.Default
                    };

                    DateTime date = DateTime.Now;

                    // Discord embed limit description is 4096 characters.
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

                    // Uma vez que o texto foi gerado, ele já fica registrado como o ultimo histórico de chat.
                    ChatHistoric? historic = guild.LastChatHistoric(user, channel: channel.Id);

                    if (historic != null)
                    {
                        ChatValueUsage? usage = historic.Usage;
                        ChatPriceUsage? price = model.Price;

                        if (usage != null && price != null)
                        {
                            decimal inputPrice = usage.InputTokens * price.InputPricePerToken;
                            decimal outputPrice = usage.OutputTokens * price.OutputPricePerToken;

                            priceEmbed = new EmbedBuilder()
                                // Input Field
                                .AddField("Tokens", usage.InputTokens, true)
                                .AddField(guild.GetTranslation(LangKeys.Request), $"$ {Util.FormatPrice(inputPrice)}", true)
                                // Broke Line
                                .AddField("\u200B", "\u200B", false)
                                // Output Field
                                .AddField("Tokens", usage.OutputTokens, true)
                                .AddField(guild.GetTranslation(LangKeys.Response), $"$ {Util.FormatPrice(outputPrice)}", true)
                                // Broke Line
                                .AddField("\u200B", "\u200B", false)
                                // Total Field
                                .AddField("Tokens", usage.TotalTokens(), true)
                                .AddField(guild.GetTranslation(LangKeys.Total), $"$ {Util.FormatPrice(inputPrice + outputPrice)}", true)
                                // Footer
                                .WithFooter(guild.GetTranslation(LangKeys.PriceLowerCache));
                        }
                    }
                    break;

                case ModelType.Image:

                    // Futuramente vai dar para personalizar.
                    ImageGenerationOptions options = new()
                    {
                        Quality = GeneratedImageQuality.Standard,
                        Size = GeneratedImageSize.W1024xH1024,
                        Style = GeneratedImageStyle.Natural,
                        ResponseFormat = GeneratedImageFormat.Uri
                    };

                    string responseImageUrl = await AiService.GenerateImageUrlAsync(guild, guildUser, model, options, channel.Id, prompt);

                    // Como pode retornar um url ou mensagem de erro, fazemos essa verificação.
                    if (Util.IsValidUrl(responseImageUrl))
                    {
                        embed.WithDescription(guild.GetTranslation(LangKeys.Success))
                            .WithColor(Color.Green);

                        embed.WithImageUrl(responseImageUrl);
                    }
                    else
                    {
                        embed.WithDescription(responseImageUrl)
                            .WithColor(Color.Red);
                    }
                    break;
            }

            List<Embed> embeds = new List<Embed>();

            if (priceEmbed != null)
            {
                embeds.Add(priceEmbed.Build());
            }

            // É adicionado o embed principal no final para que ele seja o último a ser exibido.
            embeds.Add(embed.Build());

            await botMessage.ModifyAsync(message => message.Embeds = embeds.ToArray());
        }
        catch (Exception e)
        {
            await LogUtil.ErrorAsync(nameof(MessageReceivedHandler), "Can't proccess the content receiver.", e.Message);
        }
    }
}