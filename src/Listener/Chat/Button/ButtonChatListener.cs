using Ares.src.Backend.Data;
using Ares.src.Objects;
using Ares.src.Util.Extra;
using Discord;
using Discord.WebSocket;

namespace Ares.src.Listener.Chat.Button
{
    internal class ButtonChatListener
    {

        public ButtonChatListener(DiscordSocketClient client)
        {
            client.ButtonExecuted += ButtonExecutedHandler;
        }

        /* CLOSE BUTTON */

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

                GuildData? data = Core.GuildData;

                if (data == null)
                {
                    await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                    return;
                }

                Guild.Guild? guild = await data.Fetch(args.GuildId.GetValueOrDefault());

                if (guild == null)
                {
                    await args.FollowupAsync("Ops! Parece que o servidor atual não foi configurado no banco de dados.");
                    return;
                }

                var channel = await args.GetChannelAsync() as SocketTextChannel;

                if (channel != null)
                {
                    await guild.EndConversation(user);

                    await args.FollowupAsync("Obrigado por usar **Ares**! A fechar a conversa...");

                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await channel.DeleteAsync();
                }
                else
                {
                    await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                }
            }
            catch (Exception e)
            {
                await args.FollowupAsync(Constant.UNABLE_PERFORM_TASK);
                await LogUtil.ErrorAsync("EXCEPTION", "Unable to close chat.", e.Message);
            }
        }
    }
}