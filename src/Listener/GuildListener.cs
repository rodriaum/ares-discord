using Discord.WebSocket;
using Discord;
using Ares.Database.Collection;
using Ares.Database.Model;

namespace Ares.Listener;

class GuildListener
{
    private static DiscordSocketClient? _client;

    public GuildListener(DiscordSocketClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client), "Client cannot be null");

        _client = client;
        _client.Ready += Ready;
        _client.GuildAvailable += GuildAvailable;
        _client.GuildUnavailable += GuildUnavailable;
    }

    private async Task Ready()
    {
        if (_client == null) return;

        IReadOnlyCollection<IGuild>? guilds = _client.Guilds;
        if (guilds == null || guilds.Count == 0) return;

        GuildCollection? data = Program.GuildCollection;
        if (data == null) return;

        foreach (IGuild iguild in guilds)
        {
            Guild? guild = await data.FetchAsync(iguild.Id, saveInRedis: true);
            if (guild != null) continue;

            await data.SaveAsync(iguild.Id.ToString());
        }
    }

    private async Task GuildAvailable(SocketGuild guild)
    {
        if (guild == null) return;

        GuildCollection? data = Program.GuildCollection;
        if (data == null) return;

        await data.SaveAsync(guild.Id);
    }

    private async Task GuildUnavailable(SocketGuild guild)
    {
        if (guild == null) return;

        GuildCollection? data = Program.GuildCollection;
        if (data == null) return;

        await data.DeleteCache(guild.Id);
    }
}