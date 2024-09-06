using Ares.Objects.OpenAI;
using Lombok.NET;
using OpenAI.Chat;

namespace Ares.Guild.ChatData
{
    [AllArgsConstructor]
    internal partial class GuildChatData
    {
        public required Dictionary<ulong, OpenAiModel> ConversationModels {  get; set; }
        public required Dictionary<ulong, List<ChatMessage>> ConversationHistorics {  get; set; }
        public required Dictionary<ulong, List<ChatCompletion>> CompletionHistorics { get; set; }
    }
}