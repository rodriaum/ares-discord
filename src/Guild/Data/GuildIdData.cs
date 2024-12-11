using Lombok.NET;

namespace Ares.src.Guild.ChatData
{
    [AllArgsConstructor]
    public partial class GuildIdData
    {
        public ulong MemberRoleId { get; set; }
        public ulong UsageRoleId { get; set; }
        public ulong ExclusiveRoleId { get; set; }

        public ulong SetupChannelId { get; set; }

        public ulong ChatsCategoryId { get; set; }
    }
}