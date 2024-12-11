using Discord.WebSocket;
using System.Text;
using OpenAI.Chat;
using System.ClientModel;
using Ares.src.Objects;
using Ares.src.Util.Extra;
using Ares.src.Objects.OpenAI.Model;


namespace Ares.src.OpenAi
{
    internal class OpenAiService
    {
        public static List<OpenAiModel> OpenAiModels = new List<OpenAiModel>();

        public async static Task<string> GenerateConversation(Guild.Guild guild, SocketGuildUser user, OpenAiModel model, string prompt)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (guild != null)
                {
                    await guild.AddConversationAsync(user, new UserChatMessage(prompt));

                    ChatClient client = new ChatClient(model.Model, guild.Information.OpenAiToken);
                    ChatCompletion completion = await client.CompleteChatAsync(guild.Messages(user));

                    await guild.AddConversationAsync(user, new AssistantChatMessage(completion));
                    await guild.AddCompletion(user, completion);

                    sb.Append(completion).Append("\n");
                }
            }
            catch (ClientResultException e)
            {
                Console.WriteLine(e.Message);

                if (e.Status == 429)
                {
                    if (e.Message.Contains("insufficient_quota"))
                        sb.Append("A cota disponível do servidor atual foi excedida.");
                    else
                        sb.Append("Limite de solicitações excedido.");
                }
                else
                {
                    await LogUtil.ErrorAsync("EXCEPTION", "Unable to generate a conversation.", e.Message);
                    sb.Append("Desculpe, mas não posso continuar com esta conversa. Agradeço a sua compreensão. 🙏");
                }
            }
            catch (Exception e)
            {
                await LogUtil.ErrorAsync("EXCEPTION", "Unable to generate a conversation.", e.Message);
                sb.Append(Constant.UNABLE_PERFORM_TASK);
            }

            return sb.ToString();
        }
    }
}