using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Ares.src.Utils.Extra;
using Ares.src.Guild.Information;
using Ares.src.Guild.Config;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Objects.Model;
using Ares.src.Backend.Data;
using MongoDB.Driver;
using Ares.src.Objects.Language;

namespace Ares.src.Listener.Chat;

internal class SelectedChatListener
{
    private static DiscordSocketClient? _client { get; set; }

    public SelectedChatListener(DiscordSocketClient client)
    {
        client.SelectMenuExecuted += SelectMenuHandler;
        _client = client;
    }

    private async Task SelectMenuHandler(SocketMessageComponent args)
    {
        if (!args.Data.CustomId.StartsWith("chat-menu-")) return;

        await args.DeferLoadingAsync(true);

        try
        {
            SocketUser user = args.User;
            ulong guildId = args.GuildId.GetValueOrDefault();

            if (_client == null || user == null)
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
            const int maxAttempts = 3;

            for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
            {
                await args.FollowupAsync($"Tentando criar servidor no banco de dados... {attempts}/{maxAttempts}");
                await Task.Delay(1500);
                guild = await data.Save(guildId);
            }

            if (guild == null)
            {
                await args.FollowupAsync("Ops! Não foi possível criar o servidor no banco de dados.");
                return;
            }

            SocketGuild socketGuild = _client.GetGuild(guildId);

            if (socketGuild == null)
            {
                await args.FollowupAsync(Constant.UNABLE_GET_MEMBER);
                return;
            }

            GuildInformation information = guild.Information;

            if (information == null)
            {
                await args.FollowupAsync(guild.GetTranslation(LangKeys.ServerNotFoundDatabase));
                return;
            }

            GuildConfigData? gid = information.Config;

            if (gid == null)
            {
                await args.FollowupAsync(guild.GetTranslation(LangKeys.CouldNotFindInfoID));
                return;
            }

            IRole usageRole = socketGuild.GetRole(gid.UsageRoleId);

            if (usageRole == null)
            {
                await args.FollowupAsync(guild.GetTranslation(LangKeys.RoleEliminated));
                return;
            }

            SocketGuildUser member = socketGuild.GetUser(user.Id);

            if (!member.Roles.Contains(usageRole))
            {
                await args.FollowupAsync(guild.GetTranslation(LangKeys.RoleMissing).Replace("{0}", usageRole.Mention));
                return;
            }

            if (guild.HasActiveUserConversation(user))
            {
                await args.FollowupAsync(guild.GetTranslation(LangKeys.ActiveConversation));
                return;
            }

            ChatModel? model = ChatModel.GetByModel(args.Data.Values.First());

            if (model == null)
            {
                await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                return;
            }

            if (!model.Available)
            {
                await args.FollowupAsync(guild.GetTranslation(LangKeys.ModelUnavailable));
                return;
            }

            SocketCategoryChannel category = socketGuild.GetCategoryChannel(gid.ChatsCategoryId);
            RestTextChannel channel = await socketGuild.CreateTextChannelAsync("\uD83E\uDDFF┃" + user.GlobalName, properties => properties.CategoryId = category.Id);

            ChatInfo info = new ChatInfo
                (
                    active: true,
                    channel: channel.Id,
                    model: model.Model
                );

            if (!await guild.CreateChatData(user, info))
            {
                await channel.DeleteAsync();
                await Task.Delay(500);
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle($"Olá, {user.GlobalName}")
                .WithColor(Color.Green)
                .WithFooter(footer => footer.WithText($"{DateTime.Now.Year} | {socketGuild.Name}"));

            embed.AddField(guild.GetTranslation(LangKeys.FieldModel), model.DisplayName);
            embed.AddField(guild.GetTranslation(LangKeys.FieldRules), guild.GetTranslation(LangKeys.ChatDescriptionRules));
            embed.AddField(guild.GetTranslation(LangKeys.FieldTime), guild.GetTranslation(LangKeys.ChatDescriptionTime));

            switch (model.Type)
            {
                case ModelType.Chat:
                    embed.AddField(guild.GetTranslation(LangKeys.FieldHistory), guild.GetTranslation(LangKeys.HistoryChatDesc));
                    embed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionDefault));
                    break;

                case ModelType.Image:
                    embed.AddField(guild.GetTranslation(LangKeys.FieldHistory), guild.GetTranslation(LangKeys.HistoryImageDesc));
                    embed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionImage));
                    break;

                default:
                    embed.WithDescription(guild.GetTranslation(LangKeys.ChatDescriptionDefault));
                    break;
            }

            ButtonBuilder button = new ButtonBuilder()
               .WithLabel(guild.GetTranslation(LangKeys.ButtonEndChat))
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

            await args.FollowupAsync(guild.GetTranslation(LangKeys.SuccessChatCreated).Replace("{0}", channel.Mention));
        }
        catch (Exception e)
        {
            await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
            await LogUtil.ErrorAsync("SelectException", "Unable to process chat model choice.", e.Message);
        }
    }
}