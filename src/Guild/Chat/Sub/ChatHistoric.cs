using Ares.src.Utils.Extra;
using OpenAI.Chat;

namespace Ares.src.Guild.Chat.Sub
{
    public class ChatHistoric
    {
        public bool Active { get; set; }

        public string? Model { get; set; }
        public string? Prompt { get; set; }
        public ChatMessageRole? Role { get; set; }

        public ChatContent? Content { get; set; }
        public ChatTokenUsage? Usage { get; set; }

        public long Timestamp { get; set; }

        public ChatHistoric(string model = "", string prompt = "", ChatMessageRole role = ChatMessageRole.System, ChatContent? content = null, ChatTokenUsage? usage = null, long timestamp = -1)
        {
            this.Active = true;
            this.Model = model;
            this.Prompt = prompt;
            this.Role = role;
            this.Content = content;
            this.Usage = usage;

            if (timestamp == -1)
            {
                this.Timestamp = TimeUtil.CurrentTimeMillis();
            }
        }
    }
}
