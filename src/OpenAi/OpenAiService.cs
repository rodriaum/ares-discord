using Discord.WebSocket;
using System.Text;
using OpenAI.Chat;
using System.ClientModel;
using Ares.src.Objects;
using Ares.src.Util.Extra;
using Ares.src.Objects.OpenAI.Model;
using Ares.src.Objects.OpenAI.Error;
using Discord;
using Microsoft.VisualBasic;
using Ares.src.Guild.Information;
using Ares.src.Guild.ChatData;


namespace Ares.src.OpenAi
{
    public class OpenAiService
    {
        public static List<OpenAiModel> OpenAiModels = new List<OpenAiModel>();

        public async static Task<string> GenerateConversation(Guild.Guild guild, SocketGuildUser user, OpenAiModel model, string prompt)
        {
            StringBuilder sb = new StringBuilder();

            GuildInformation information = guild.Information;

            ChatMessage userChatMessage = new UserChatMessage(prompt);

            try
            {
                if (guild != null)
                {
                    string token = information.OpenAiToken;

                    if (!string.IsNullOrEmpty(token))
                    {
                        await guild.UpdateConversationAsync(user, userChatMessage);


                        ChatClient client = new ChatClient(model.Model, token);
                        ChatCompletion completion = await client.CompleteChatAsync(guild.Messages(user));

                        await guild.UpdateConversationAsync(user, new AssistantChatMessage(completion));
                        await guild.AddCompletion(user, completion);

                        sb.Append(completion).Append("\n");

                    }
                    else
                    {
                        sb.Append("Ops! Parece que o servidor atual não tem um token pré configurado.").Append("\n");
                    }
                }
            }
            catch (ClientResultException e)
            {
                try
                {
                    OpenAiError? error = OpenAiError.TryGetErrorByMessage(e.Message);

                    if (error != null)
                    {
                        sb.AppendLine(error.Cause);


                        // Start:Provisório

                        GuildIdData? gid = information.GuildIdData;

                        if (gid != null)
                        {
                            ITextChannel channel = user.Guild.GetTextChannel(gid.LogChannelId);

                            if (channel != null)
                            {
                                EmbedBuilder embed = new EmbedBuilder();

                                embed.Title = "Falha ao Gerar Pedido";
                                embed.Color = Color.Gold;
                                embed.WithCurrentTimestamp();

                                embed.AddField("User", user.Mention, false);
                                embed.AddField("Model", model.DisplayName, false);
                                embed.AddField("Prompt", prompt, false);

                                embed.AddField("Code", $"`{error.Code}`", false);
                                embed.AddField("Overview", error.Overview, false);
                                embed.AddField("Cause", error.Cause, false);
                                embed.AddField("Solution", error.Solution, false);
                                embed.AddField("Type", error.Type, false);

                                await channel.SendMessageAsync(embed: embed.Build());
                            }
                        }

                        // End:Provisório


                    }
                    else
                    {
                        sb.Append(Constant.UNABLE_PERFORM_TASK);
                    }
                }
                catch (Exception) { }

                if (!await guild.RemoveConversationAsync(user, userChatMessage))
                {
                    await LogUtil.ErrorAsync("EXCEPTION", "Não foi possível eliminar o histórico após um erro.", e.Message);
                }

            }
            catch (Exception e)
            {
                if (!await guild.RemoveConversationAsync(user, userChatMessage))
                {
                    await LogUtil.ErrorAsync("EXCEPTION", "Não foi possível eliminar o histórico após um erro.", e.Message);
                }

                await LogUtil.ErrorAsync("EXCEPTION", "Unable to generate a conversation.", e.Message);
                sb.Append(Constant.UNABLE_PERFORM_TASK);
            }

            return sb.ToString();
        }
    }
}