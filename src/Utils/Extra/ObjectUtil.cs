using Ares.src.Guild.Chat.Sub;
using OpenAI.Chat;

namespace Ares.src.Utils.Extra
{
    public class ObjectUtil
    {
        public static ChatHistoric BuildChatHistoric(string prompt, ChatCompletion completion)
        {
            var content = completion.Content[0];

            return new ChatHistoric
            (
                completion.Model,
                prompt,
                completion.Role,
                new ChatContent(kind: content.Kind, text: content.Text, imageUrl: content.ImageUri.AbsoluteUri),
                completion.Usage,
                completion.CreatedAt.Ticks
            );
        }

        public static ChatMessage CreateUserMessage(ChatHistoric historic)
        {
            return ChatMessage.CreateUserMessage(historic.Prompt);
        }

        public static ChatMessage? CreateSystemMessage(ChatHistoric historic)
        {
            var content = historic.Content;
            if (content == null) return null;

            return ChatMessage.CreateSystemMessage(content.Text());
        }
    }
}