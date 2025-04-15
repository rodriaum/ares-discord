using Ares.Core.Models.Database;

namespace Ares.Core.Interfaces;

internal interface ICollection
{
    Task<Guild?> SaveAsync(string id);
    Task<Guild?> SaveAsync(ulong id);

    Task<Guild?> FetchAsync(string id, bool saveInRedis = false);
    Task<Guild?> FetchAsync(ulong id, bool saveInRedis = false);

    Task<bool> UpdateAsync(Guild guild, string field);

    Task DeleteCache(string id);
    Task DeleteCache(ulong id);
}