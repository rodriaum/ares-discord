using Ares.src.Backend.Data;
using Ares.src.Objects;
using Ares.src.Objects.OpenAI.Model;
using Ares.src.Objects.OpenAI.Model.Category;
using Ares.src.Util.Extra;
using Discord;
using Discord.Rest;
using Discord.WebSocket;


namespace Ares.src.Listener.Chat
{
    internal class ReceivedContentListener
    {
        private static DiscordSocketClient? Client { get; set; }

        public ReceivedContentListener(DiscordSocketClient client)
        {
            client.MessageReceived += MessageReceivedHandler;
            Client = client;
        }

        /* ON RECEIVED PROMPT MESSAGE CHAT */

        private async Task MessageReceivedHandler(SocketMessage args)
        {
            try
            {
                SocketTextChannel? channel = args.Channel as SocketTextChannel;
                if (channel == null) return;

                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Inteligência Artificial")
                    .WithDescription("A processar...");

                IUser user = args.Author;

                if (Client == null || user == null)
                {
                    await channel.SendMessageAsync(embed: embed.WithDescription(Constant.UNABLE_GET_MEMBER).Build());
                    return;
                }

                if (user.Id.Equals(Client.CurrentUser.Id)) return;
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
                    await channel.SendMessageAsync(embed: embed.WithDescription("Ops! Parece que o servidor atual não foi configurado no banco de dados.").Build());
                    return;
                }

                if (!(channel.CategoryId.Equals(guild.GuildIdData.ChatsCategoryId) && guild.HasUserConversation(user))) return;

                // O método só é ivocado aqui porque ele iria enviar mensagem sem a verificação de cima estar finalizada.
                RestUserMessage botMessage = await channel.SendMessageAsync(embed: embed.Build());

                if (!channel.Name.Contains(user.GlobalName))
                {
                    await botMessage.ModifyAsync(message => message.Embed = embed.WithDescription(Constant.UNABLE_GET_MEMBER).Build());

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await message.DeleteAsync();
                    return;
                }

                IRole exclusiveRole = socketGuild.GetRole(guild.GuildIdData.ExclusiveRoleId);

                OpenAiModel? model = guild.GetModelByUser(user);

                if (model == null)
                {
                    return;
                }

                //int totalQuestions = guild

                switch (model.OpenAiModelCategory)
                {
                    case OpenAiModelCategory.CHAT:
                        string response = await OpenAi.GenerateConversation(guild, socketGuild.GetUser(user.Id), model, message.Content);

                        embed.WithDescription(response);
                        break;

                    case OpenAiModelCategory.IMAGE:
                        break;
                }

                await botMessage.ModifyAsync(message => message.Embed = embed.Build());
            }
            catch (Exception e)
            {
                await LogUtil.ErrorAsync("EXCEPTION", "", e.Message);
            }
        }
    }
}