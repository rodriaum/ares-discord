using Lombok.NET;

namespace Ares.src.Guild.Config
{
    [AllArgsConstructor]
    public partial class GuildConfigData
    {
        public ulong MemberRoleId { get; set; }
        public ulong UsageRoleId { get; set; }
        public ulong ExclusiveRoleId { get; set; }

        public ulong SetupChannelId { get; set; }
        public ulong LogChannelId { get; set; }

        public ulong ChatsCategoryId { get; set; }
    }
}