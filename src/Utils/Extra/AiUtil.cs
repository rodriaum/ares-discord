using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Chat;
using OpenAI.Chat;

namespace Ares.src.Utils.Extra
{
    public class AiUtil
    {
        /// <summary>
        /// Constrói um histórico de chat para Anthropic.
        /// </summary>
        public static ChatHistoric BuildAnthropicChatHistoric(string prompt, ulong channel, MessageResponse response)
        {
            return new ChatHistoric
            (
                channel: channel,
                model: response.Model,
                prompt: prompt,
                response: response.Message.ToString(),
                usage: new ChatValueUsage(response.Usage.OutputTokens, response.Usage.InputTokens),
                role: ConvertAnthropicRole(response.Role)
            );
        }

        /// <summary>
        /// Obtém mensagens do histórico de chat da Anthropic.
        /// </summary>
        public static List<Message> GetChatAnthropicMessages(List<ChatHistoric>? historics)
        {
            if (historics == null || historics.Count == 0)
            {
                return new List<Message>();
            }

            List<Message> messages = new List<Message>();

            foreach (ChatHistoric historic in historics)
            {
                if (!historic.Active) continue;

                if (!string.IsNullOrWhiteSpace(historic.Prompt))
                {
                    messages.Add(new Message(RoleType.User, historic.Prompt));
                }

                if (!string.IsNullOrWhiteSpace(historic.Response))
                {
                    messages.Add(new Message(RoleType.Assistant, historic.Response));
                }
            }

            return messages;
        }

        /// <summary>
        /// Converte um <see cref="RoleType"/> da Anthropic para um <see cref="ChatRole"/>.
        /// </summary>
        public static ChatRole ConvertAnthropicRole(RoleType role)
        {
            return role switch
            {
                RoleType.User => ChatRole.User,
                RoleType.Assistant => ChatRole.Assistant,
                _ => ChatRole.None
            };
        }

        /// <summary>
        /// Constrói um histórico de chat para OpenAI.
        /// </summary>
        public static ChatHistoric BuildOpenAiChatHistoric(string prompt, ulong channel, ChatCompletion completion)
        {
            var content = completion.Content[0];
            string? imageUrl = content.ImageUri?.AbsoluteUri;

            return new ChatHistoric
            (
                channel: channel,
                model: completion.Model,
                prompt: prompt,
                response: content.Text,
                imageUrl: imageUrl,
                role: ConvertOpenAiRole(completion.Role),
                usage: new ChatValueUsage(completion.Usage.OutputTokens, completion.Usage.InputTokens),
                timestamp: completion.CreatedAt.Ticks
            );
        }

        /// <summary>
        /// Cria uma mensagem de usuário a partir do histórico.
        /// </summary>
        public static ChatMessage CreateUserMessage(ChatHistoric historic)
        {
            return ChatMessage.CreateUserMessage(historic.Prompt);
        }

        /// <summary>
        /// Cria uma mensagem de sistema a partir do histórico.
        /// </summary>
        public static ChatMessage? CreateSystemMessage(ChatHistoric historic)
        {
            return ChatMessage.CreateSystemMessage(historic.Response);
        }

        /// <summary>
        /// Obtém mensagens do histórico de chat da OpenAI.
        /// </summary>
        public static List<ChatMessage> GetChatOpenAiMessages(List<ChatHistoric>? historics)
        {
            if (historics == null || historics.Count == 0)
            {
                return new List<ChatMessage>();
            }

            List<ChatMessage> messages = new List<ChatMessage>();

            foreach (ChatHistoric historic in historics)
            {
                if (!historic.Active) continue;

                if (!string.IsNullOrWhiteSpace(historic.Prompt))
                {
                    messages.Add(new UserChatMessage(historic.Prompt));
                }

                if (!string.IsNullOrWhiteSpace(historic.Response))
                {
                    messages.Add(new AssistantChatMessage(historic.Response));
                }
            }

            return messages;
        }

        /// <summary>
        /// Converte um <see cref="ChatMessageRole"/> da OpenAI para um <see cref="ChatRole"/>.
        /// </summary>
        public static ChatRole ConvertOpenAiRole(ChatMessageRole role)
        {
            return role switch
            {
                ChatMessageRole.System => ChatRole.System,
                ChatMessageRole.User => ChatRole.User,
                ChatMessageRole.Assistant => ChatRole.Assistant,
                _ => ChatRole.None
            };
        }
    }
}