/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */


/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models.Database;

namespace Ares.Core.Database.Repository;

internal class GuildManager
{
    private readonly Dictionary<string, Guild> _guilds = new Dictionary<string, Guild>();

    public IEnumerable<Guild> Fetch()
    {
        return _guilds.Values.ToList();
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
        return _guilds.GetValueOrDefault(guildId);
    }

    public void Save(Guild guild)
    {
        _guilds.TryAdd(guild.Id, guild);
    }

    public void Delete(string guildId)
    {
        _guilds.Remove(guildId);
    }
}