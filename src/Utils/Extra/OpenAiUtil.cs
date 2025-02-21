using Ares.src.Guild.Chat.Sub;
using Ares.src.Service.Chat;
using OpenAI.Chat;

namespace Ares.src.Utils.Extra
{
    public class OpenAiUtil
    {
        public static ChatHistoric BuildChatHistoric(string prompt, ChatCompletion completion)
        {
            var content = completion.Content[0];

            string? imageUrl = null;
            Uri uri = content.ImageUri;

            if (uri != null)
            {
                imageUrl = uri.AbsoluteUri;
            }

            return new ChatHistoric
            (
                model: completion.Model,
                prompt: prompt,
                response: content.Text,
                imageUrl: imageUrl,
                role: ConvertOpenAiRole(completion.Role),
                usage: new ChatValueUsage(completion.Usage.OutputTokens, completion.Usage.InputTokens),
                timestamp: completion.CreatedAt.Ticks
            );
        }

        public static ChatMessage CreateUserMessage(ChatHistoric historic)
        {
            return ChatMessage.CreateUserMessage(historic.Prompt);
        }

        public static ChatMessage? CreateSystemMessage(ChatHistoric historic)
        {
            return ChatMessage.CreateSystemMessage(historic.Response);
        }

        public static List<ChatMessage> GetChatMessages(List<ChatHistoric>? historics)
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
        /// <param name="role">O papel da mensagem no chat da OpenAI.</param>
        /// <returns>O papel correspondente em <see cref="ChatRole"/>.</returns>
        /// <exception cref="ChatRole.None">Se o papel não for reconhecido.</exception>
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