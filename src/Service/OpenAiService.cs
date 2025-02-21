using Discord.WebSocket;
using OpenAI.Chat;
using Discord;
using Ares.src.Guild.Information;
using OpenAI.Images;
using Ares.src.Manager;
using System.ClientModel;
using Ares.src.Utils.Extra;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Model;


namespace Ares.src.Service
{
    public class OpenAiService : OpenAiManager
    {


        // Futuro: Fazer retornar o url e um bool de sucesso.

        public async static Task<string> GenerateImageUrlAsync(Guild.Guild guild, SocketGuildUser user, ChatModel model, ImageGenerationOptions options, string prompt)
        {
            HandleVerifyParameters(guild, user, model, prompt);

            // Verificação do tipo de modelo
            if (model.Type != ModelType.Image)
            {
                return "Parece que houve um problema na identificação do modelo. Tente novamente!";
            }

            // Obtenção das informações da guilda
            GuildInformation information = guild.Information;

            // Validação do token
            string token = information.OpenAiToken;
            if (string.IsNullOrEmpty(token))
            {
                return "Ops! Parece que o servidor atual não tem um token pré-configurado.";
            }
            
            try
            {
                // Inicializar cliente de imagem
                ImageClient client = new ImageClient(model.Model, token);
                GeneratedImage image = await client.GenerateImageAsync(prompt, options);

                return image.ImageUri.OriginalString;
            }
            catch (Exception e)
            {
                return Constant.UNABLE_PERFORM_TASK;
            }
        }


        // Futuro: Fazer retornar o texto e um bool de sucesso.
        public async static Task<string> GenerateConversationAsync(Guild.Guild guild, SocketGuildUser user, ChatModel model, string prompt)
        {
            HandleVerifyParameters(guild, user, model, prompt);

            // Verificação do tipo de modelo
            if (model.Type != ModelType.Chat)
            {
                return "Parece que houve um problema na identificação do modelo. Tente novamente!";
            }

            // Obtenção das informações da guilda
            GuildInformation information = guild.Information;

            UserChatMessage userChatMessage = new UserChatMessage(prompt);

            // O nome do usuário na conversa é o nome do Discord.
            userChatMessage.ParticipantName = user.GlobalName;

            // Validação do token
            string token = information.OpenAiToken;
            if (string.IsNullOrEmpty(token))
            {
                return "Ops! Parece que o servidor atual não tem um token pré-configurado.";
            }

            ChatHistoric? historic = null;

            try
            {
                ChatClient client = new ChatClient(model.Model, token);
                
                List<ChatHistoric>? historics = guild.ChatHistorics(user);
                List<ChatMessage> messages = OpenAiUtil.GetChatMessages(historics);

                ChatCompletion completion = await client.CompleteChatAsync(messages);

                historic = OpenAiUtil.BuildChatHistoric(prompt, completion);
                await guild.SaveHistoricAsync(user, historic);

                // Processar resposta do chat
                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        return completion.ToString();

                    case ChatFinishReason.Length:
                        return "Não será possível prosseguir porque o limite de token estabelecido pelo servidor atual foi excedido.";

                    case ChatFinishReason.ContentFilter:
                        return "Não foi possível gerar porque o sistema identificou palavras ofensivas no canal atual.";

                    case ChatFinishReason.FunctionCall:
                        return "Não foi possível gerar porque o sistema está lento. (FunctionCall)";

                    default:
                        return $"Não foi possível gerar a resposta. Motivo: {completion.FinishReason}";
                }
            }
            catch (Exception e)
            {
                
                if (historic != null && !await guild.RemoveConversationAsync(user, historic))
                {
                    throw new Exception("Não foi possível remover a conversa do usuário após um problema interno.", e);
                }
                
                return Constant.UNABLE_PERFORM_TASK;
            }
        }

        private static void HandleVerifyParameters(Guild.Guild guild, IGuildUser user, ChatModel model, String prompt)
        {
            // Validação de parâmetros
            if (guild == null)
                throw new ArgumentNullException(nameof(guild), "Houve um problema interno ao identificar uma guilda. Por favor, verifique se a guilda foi fornecida corretamente.");

            if (user == null)
                throw new ArgumentNullException(nameof(user), "Houve um problema interno ao identificar um usuário. Por favor, verifique se o usuário foi fornecido corretamente.");

            if (model == null)
                throw new ArgumentNullException(nameof(model), "Houve um problema interno ao identificar o modelo. Por favor, verifique se o modelo foi fornecido corretamente.");

            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentNullException(nameof(prompt), "Houve um problema interno ao identificar o prompt. Por favor, verifique se o prompt foi fornecido corretamente.");
        }
    }
}