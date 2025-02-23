using Ares.src.Guild.Information;
using Ares.src.Utils.Extra;
using Ares.src.Utils;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using OpenAI.Images;
using Ares.src.Guild.Config;
using Ares.src.Service;
using Ares.src.Service.Model;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Chat;

namespace Ares.src.Listener.Chat
{
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

                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Inteligência Artificial")
                    .WithDescription("A processar... 🔃")
                    .WithColor(Color.Gold)
                    .WithFooter("Pode demorar até minutos");

                IUser user = args.Author;

                if (_client == null || user == null)
                {
                    await channel.SendMessageAsync(embed: embed.WithDescription(Constant.UNABLE_GET_MEMBER).Build());
                    return;
                }

                if (user.Id.Equals(_client.CurrentUser.Id)) return;
                if (args is not SocketUserMessage message) return;

                GuildData? data = Core.GuildData;

                if (data == null)
                {
                    await channel.SendMessageAsync(embed: embed.WithDescription(Constant.UNABLE_PERFORM_TASK).Build());
                    return;
                }

                SocketGuild socketGuild = channel.Guild;

                if (socketGuild == null)
                {
                    await channel.SendMessageAsync(embed: embed.WithDescription(Constant.UNABLE_GET_MEMBER).Build());
                    return;
                }

                Guild.Guild? guild = await data.Fetch(socketGuild.Id);

                if (guild == null)
                {
                    // await channel.SendMessageAsync(embed: embed.WithDescription("Ops! Parece que o servidor atual não foi configurado no banco de dados.").Build());
                    return;
                }

                GuildInformation information = guild.Information;

                if (information == null)
                {
                    await channel.SendMessageAsync(embed: embed.WithDescription("Não foi possível encontrar as informações da guilda atual no banco de dados.").Build());
                    return;
                }

                GuildConfigData? gcd = information.Config;

                if (gcd == null)
                {
                    await channel.SendMessageAsync(embed: embed.WithDescription("Não foi possível encontrar as informações sobre os IDs.").Build());
                    return;
                }

                if (!(channel.CategoryId.Equals(gcd.ChatsCategoryId) && guild.HasActiveUserConversation(user))) return;

                // O método só é ivocado aqui porque ele iria enviar mensagem sem a verificação de cima estar finalizada.
                RestUserMessage botMessage = await channel.SendMessageAsync(embed: embed.Build());

                // Verifica se o canal em que o usuário enviou a mensagem é dele. (Futuramente pode ser verificado com banco de dados)
                if (!channel.Name.Contains(user.GlobalName.ToLower())) return;

                IRole exclusiveRole = socketGuild.GetRole(gcd.ExclusiveRoleId);

                ChatModel? model = guild.GetLastModelByUser(user);
                if (model == null) return;

                SocketGuildUser guildUser = socketGuild.GetUser(user.Id);
                string prompt = message.Content;

                EmbedBuilder? priceEmbed = null;

                switch (model.Type)
                {
                    case ModelType.Chat:
                        string responseText = await AiService.GenerateConversationAsync(guild, guildUser, model, channel.Id, prompt);

                        Color color = model.Category switch
                        {
                            ModelCategory.OpenAI => Color.Green,
                            ModelCategory.Anthropic => Color.Orange,
                            ModelCategory.DeepSeek => Color.Blue,
                            _ => Color.Default
                        };

                        embed.WithDescription(responseText)
                            .WithColor(color)
                            .WithFooter($"Ares - {model.DisplayName}");

                        ChatHistoric? historic = guild.LastChatHistoric(user);

                        if (historic != null)
                        {
                            ChatValueUsage? usage = historic.Usage;
                            ChatPriceUsage? price = model.Price;

                            if (usage != null && price != null)
                            {
                                double inputPrice = usage.InputTokens * price.InputPricePerToken;
                                double outputPrice = usage.OutputTokens * price.OutputPricePerToken;

                                double totalPrice = Math.Round((inputPrice + outputPrice), 2);

                                priceEmbed = new EmbedBuilder()
                                    .AddField("Tokens", usage.TotalTokens())
                                    .AddField("Total", $"$ {totalPrice}")
                                    .WithFooter($"Não inclui mensagens guardadas");
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

                        string responseImageUrl = await AiService.GenerateImageUrlAsync(guild, guildUser, model, options, prompt);

                        // Como pode retornar um url ou mensagem de erro, fazemos essa verificação.
                        if (Util.IsValidUrl(responseImageUrl))
                        {
                            embed.WithDescription("Aqui esta a imagem solicitada:")
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
                await LogUtil.ErrorAsync("RecContException", "Can't proccess the content receiver.", e.Message);
            }
        }
    }
}