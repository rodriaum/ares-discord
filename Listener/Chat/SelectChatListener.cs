using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Ares.Backend.Data;
using Ares.Objects;
using Ares.Objects.OpenAI;
using Ares.Util.Extra;
using System.Text;
using Ares.Objects.OpenAI.Model.Category;

namespace Ares.Listener.Chat
{
    internal class SelectChatListener
    {
        private static DiscordSocketClient? Client { get; set; }

        public SelectChatListener(DiscordSocketClient client)
        {
            client.SelectMenuExecuted += SelectMenuHandler;
            Client = client;
        }

        private async Task SelectMenuHandler(SocketMessageComponent args)
        {
            if (!args.Data.CustomId.Equals("openai-chat-menu")) return;

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

                IRole usageRole = socketGuild.GetRole(guild.GuildIdData.UsageRoleId);

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


                if (guild.HasUserConversation(user))
                {
                    await args.FollowupAsync("Ops! Detectei uma conversa ativa! Para criar uma nova, termine a conversa antiga.");
                    return;
                }

                OpenAiModel? model = OpenAiModel.GetByModel(args.Data.Values.First());

                if (model == null)
                {
                    await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                    return;
                }

                if (await guild.CreateConversation(user, model))
                {
                    SocketCategoryChannel category = socketGuild.GetCategoryChannel(guild.GuildIdData.ChatsCategoryId);
                    RestTextChannel channel = await socketGuild.CreateTextChannelAsync("\uD83E\uDDFF┃" + user.GlobalName, properties => properties.CategoryId = category.Id);

                    EmbedBuilder embed = new EmbedBuilder()
                        .WithTitle($"Olá, {user.GlobalName}")
                        .WithColor(Color.Green)
                        .WithFooter(footer => footer.WithText($"{DateTime.Now.Year} | {socketGuild.Name}"));

                    switch (model.OpenAiModelCategory)
                    {
                        case OpenAiModelCategory.CHAT:
                            embed.WithDescription("Insira a sua pergunta para iniciar a conversa.");
                            break;

                        case OpenAiModelCategory.IMAGE:
                            embed.WithDescription("Insira a sua frase para gerar a imagem.");
                            break;
                    }

                    embed.AddField("Modelo", model.DisplayName);
                    embed.AddField("Regras", "Tenha respeito no canal atual.");
                    embed.AddField("Tempo", "Pode demorar até minutos para processar o seu pedido.");

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

            }
            catch (Exception e)
            {
                await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                await LogUtil.ErrorAsync("EXCEPTION", "Unable to process chat model choice.", e.Message);
            }
        }
    }
}