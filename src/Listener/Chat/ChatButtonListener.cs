using Ares.src.Database.Collection;
using Ares.src.Database.Model;
using Ares.src.Database.Model.Chat.Sub;
using Ares.src.Objects.Language;
using Ares.src.Util;
using Discord;
using Discord.WebSocket;

namespace Ares.src.Listener.Chat;

internal class ChatButtonListener
{

    public ChatButtonListener(DiscordSocketClient client)
    {
        client.ButtonExecuted += ButtonExecutedHandler;
    }

    private async Task ButtonExecutedHandler(SocketMessageComponent args)
    {
        if (!args.Data.CustomId.Equals("close-chat")) return;

        await args.DeferAsync(true);

        try
        {
            IUser user = args.User;

            if (user == null)
            {
                await args.FollowupAsync(Constant.UNABLE_GET_MEMBER);
                return;
            }

            GuildCollection? data = Program.GuildCollection;

            if (data == null)
            {
                await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                return;
            }

            Guild? guild = await data.Fetch(args.GuildId.GetValueOrDefault());

            if (guild == null)
            {
                await args.FollowupAsync("Ops! Parece que o servidor atual não foi configurado no banco de dados.");
                return;
            }

            var channel = await args.GetChannelAsync() as SocketTextChannel;

            if (channel != null)
            {
                if (!await guild.ToggleChatInfo(user, channel.Id, false))
                {
                    await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                    return;
                }

                GChatInfoModel? info = guild.ChatInfoByChannel(user, channel.Id);

                if (info == null)
                {
                    await args.FollowupAsync("Parece que você não é o proprietário desse canal.");
                    return;
                }

                await args.FollowupAsync(guild.GetTranslation(LangKeys.CloseChat));

                await Task.Delay(TimeSpan.FromSeconds(1));
                await channel.DeleteAsync();

                AresLogger.Log("Chat", $"Chat ID \"{info.Id}\" has been disabled by \"{user.Username}#{user.Discriminator}\"");
            }
            else
            {
                await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
            }
        }
        catch (Exception e)
        {
            await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
            await AresLogger.ErrorAsync("ButtonException", "Unable to close chat.", e.Message);
        }
    }
}