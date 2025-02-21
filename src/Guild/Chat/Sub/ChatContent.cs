using OpenAI.Chat;

namespace Ares.src.Guild.Chat.Sub
{
    public class ChatContent
    {
        private readonly ChatMessageContentPartKind _kind;
        private readonly string _text;
        private readonly string _imageUrl;

        public ChatContent(ChatMessageContentPartKind kind = new ChatMessageContentPartKind(), string text = "", string imageUrl = "")
        {
            _kind = kind;
            _text = text;
            _imageUrl = imageUrl;
        }

        public ChatMessageContentPartKind Kind() => _kind;

        public string Text() => _text;
        public string ImageUrl => _imageUrl;
    }
}
