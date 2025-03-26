using Ares.Database.Model;

namespace Ares.Manager;

internal class GuildManager
{
    private readonly Dictionary<string, Guild> Guilds = new Dictionary<string, Guild>();

    public IEnumerable<Guild> Fetch()
    {
        return Guilds.Values.ToList();
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
        return Guilds.GetValueOrDefault(guildId);
    }

    public void Save(Guild guild)
    {
        Guilds.TryAdd(guild.Id, guild);
    }

    public void Delete(string guildId)
    {
        Guilds.Remove(guildId);
    }
}