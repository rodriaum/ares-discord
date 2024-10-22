namespace Ares.src.Manager
{
    internal class GuildManager
    {
        private readonly Dictionary<string, Guild.Guild> GUILD_DICTIONARY = new Dictionary<string, Guild.Guild>();

        public IEnumerable<Guild.Guild> Fetch()
        {
            return GUILD_DICTIONARY.Values.ToList();
        }

        public IEnumerable<Guild.Guild> Fetch(Predicate<Guild.Guild> filter)
        {
            return Fetch().Where(it => filter(it)).ToList();
        }

        public bool IsPresent(Predicate<Guild.Guild> filter)
        {
            return Fetch().Any(it => filter(it));
        }

        public Guild.Guild? Fetch(string guildId)
        {
            return GUILD_DICTIONARY.GetValueOrDefault(guildId);
        }

        public void Save(Guild.Guild guild)
        {
            GUILD_DICTIONARY.TryAdd(guild.Id, guild);
        }

        public void Delete(string guildId)
        {
            GUILD_DICTIONARY.Remove(guildId);
        }
    }
}