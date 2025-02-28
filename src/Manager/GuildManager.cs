namespace Ares.src.Manager;

internal class GuildManager
{
    private readonly Dictionary<string, Backend.Data.Model.Guild> GUILD_DICTIONARY = new Dictionary<string, Backend.Data.Model.Guild>();

    public IEnumerable<Backend.Data.Model.Guild> Fetch()
    {
        return GUILD_DICTIONARY.Values.ToList();
    }

    public IEnumerable<Backend.Data.Model.Guild> Fetch(Predicate<Backend.Data.Model.Guild> filter)
    {
        return Fetch().Where(it => filter(it)).ToList();
    }

    public bool IsPresent(Predicate<Backend.Data.Model.Guild> filter)
    {
        return Fetch().Any(it => filter(it));
    }

    public Backend.Data.Model.Guild? Fetch(string guildId)
    {
        return GUILD_DICTIONARY.GetValueOrDefault(guildId);
    }

    public void Save(Backend.Data.Model.Guild guild)
    {
        GUILD_DICTIONARY.TryAdd(guild.Id, guild);
    }

    public void Delete(string guildId)
    {
        GUILD_DICTIONARY.Remove(guildId);
    }
}