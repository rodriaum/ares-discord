using Ares.src.Objects.OpenAI.Model;
using Lombok.NET;
using OpenAI.Chat;
using OpenAI.Images;

namespace Ares.src.Guild.ChatData
{
    [AllArgsConstructor]
    public partial class GuildChatData
    {
        public required Dictionary<ulong, OpenAiModel> ConversationModels { get; set; }
        public required Dictionary<ulong, List<ChatMessage>> ConversationHistorics { get; set; }
        public required Dictionary<ulong, List<ChatCompletion>> CompletionHistorics { get; set; }
        public required Dictionary<ulong, List<GeneratedImage>> GeneratedImageHistorics { get; set; }
    }
}