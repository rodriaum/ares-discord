using Ares.src.Database.Model;

namespace Ares.src.Manager;

internal class GuildManager
{
    private readonly Dictionary<string, Guild> GUILD_DICTIONARY = new Dictionary<string, Guild>();

    public IEnumerable<Guild> Fetch()
    {
        return GUILD_DICTIONARY.Values.ToList();
    }

    public IEnumerable<Guild> Fetch(Predicate<Guild> filter)
    {
        return Fetch().Where(it => filter(it)).ToList();
    }

    public bool IsPresent(Predicate<Guild> filter)
    {
        return Fetch().Any(it => filter(it));
    }

    public Guild? Fetch(string guildId)
    {
        return GUILD_DICTIONARY.GetValueOrDefault(guildId);
    }

    public void Save(Guild guild)
    {
        GUILD_DICTIONARY.TryAdd(guild.Id, guild);
    }

    public void Delete(string guildId)
    {
        GUILD_DICTIONARY.Remove(guildId);
    }
}