using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Ares.src.Utils.Extra;
using Ares.src.Guild.Information;
using Ares.src.Guild.Config;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Model;

namespace Ares.src.Listener.Chat
{
    internal class SelectedChatListener
    {
        private static DiscordSocketClient? Client { get; set; }

        public SelectedChatListener(DiscordSocketClient client)
        {
            client.SelectMenuExecuted += SelectMenuHandler;
            Client = client;
        }

        private async Task SelectMenuHandler(SocketMessageComponent args)
        {
            if (!args.Data.CustomId.StartsWith("chat-menu-")) return;

            await args.DeferLoadingAsync(true);

            try
            {
                SocketUser user = args.User;
                ulong guildId = args.GuildId.GetValueOrDefault();

                if (Client == null || user == null)
                {
                    await args.FollowupAsync(Constant.UNABLE_GET_MEMBER);
                    return;
                }

                GuildData? data = Core.GuildData;

                if (data == null)
                {
                    await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                    return;
                }

                Guild.Guild? guild = await data.Fetch(guildId);

                if (guild == null)
                {
                    await args.FollowupAsync("Ops! Parece que o servidor atual não foi configurado no banco de dados.");
                    return;
                }

                SocketGuild socketGuild = Client.GetGuild(guildId);

                if (socketGuild == null)
                {
                    await args.FollowupAsync(Constant.UNABLE_GET_MEMBER);
                    return;
                }

                GuildInformation information = guild.Information;

                if (information == null)
                {
                    await args.FollowupAsync("Não foi possível encontrar as informações da guilda atual no banco de dados.");
                    return;
                }

                GuildConfigData? gid = information.Config;

                if (gid == null)
                {
                    await args.FollowupAsync("Não foi possível encontrar as informações sobre os IDs.");
                    return;
                }

                IRole usageRole = socketGuild.GetRole(gid.UsageRoleId);

                if (usageRole == null)
                {
                    await args.FollowupAsync("Ops! Parece que o cargo de acesso a esse comando foi eliminado.");
                    return;
                }

                SocketGuildUser member = socketGuild.GetUser(user.Id);

                if (!member.Roles.Contains(usageRole))
                {
                    await args.FollowupAsync($"Ops! Você precisa possuir o cargo {usageRole.Mention} para executar essa tarefa.");
                    return;
                }


                if (guild.HasActiveUserConversation(user))
                {
                    await args.FollowupAsync("Ops! Detectei uma conversa ativa! Para criar uma nova, termine a conversa antiga.");
                    return;
                }

                ChatModel? model = ChatModel.GetByModel(args.Data.Values.First());

                if (model == null)
                {
                    await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                    return;
                }

                SocketCategoryChannel category = socketGuild.GetCategoryChannel(gid.ChatsCategoryId);
                RestTextChannel channel = await socketGuild.CreateTextChannelAsync("\uD83E\uDDFF┃" + user.GlobalName, properties => properties.CategoryId = category.Id);

                ChatHistoric historic = new ChatHistoric
                    (
                        channel: channel.Id,
                        model: model.Model
                    );

                if (!await guild.CreateChatData(user, historic))
                {
                    // Pode ser melhorado depois porque não é o indicado.
                    await channel.DeleteAsync();
                    await Task.Delay(500);
                    return;
                }

                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle($"Olá, {user.GlobalName}")
                    .WithColor(Color.Green)
                    .WithFooter(footer => footer.WithText($"{DateTime.Now.Year} | {socketGuild.Name}"));

                switch (model.Type)
                {
                    case ModelType.Chat:
                        embed.WithDescription("Insira a sua pergunta para iniciar a conversa.");
                        break;

                    case ModelType.Image:
                        embed.WithDescription("Insira a sua frase para gerar a imagem.");
                        break;

                    default:
                        embed.WithDescription("Insira o parâmetro para iniciar o seu pedido.");
                        break;
                }

                embed.AddField("Modelo", model.DisplayName);
                embed.AddField("Regras", "Tenha respeito no canal atual.");
                embed.AddField("Tempo", "Pode demorar até minuto(s) para processar o seu pedido.");

                ButtonBuilder button = new ButtonBuilder()
                   .WithLabel("Terminar Conversa")
                   .WithStyle(ButtonStyle.Danger)
                   .WithCustomId("close-chat");

                MessageComponent component = new ComponentBuilder()
                    .WithButton(button)
                    .Build();

                await channel.SendMessageAsync(embed: embed.Build(), components: component);

                OverwritePermissions permissions = new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    sendMessages: PermValue.Allow
                );

                await channel.AddPermissionOverwriteAsync(user, permissions);

                await args.FollowupAsync($"**Sucesso!** Acesse a sua nova conversa em {channel.Mention}");

            }
            catch (Exception e)
            {
                await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                await LogUtil.ErrorAsync("SelectException", "Unable to process chat model choice.", e.Message);
            }
        }
    }
}