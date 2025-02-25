using Discord.WebSocket;
using Discord;
using Ares.src.Backend.Data;

namespace Ares.src.Listener;

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

        GuildData? data = Core.GuildData;
        if (data == null) return;

        foreach (IGuild iguild in guilds)
        {
            Guild.Guild? guild = await data.Fetch(iguild.Id);
            if (guild != null) continue;

            await data.Save(iguild.Id.ToString());
        }
    }

    private async Task GuildAvailable(SocketGuild guild)
    {
        if (guild == null) return;

        GuildData? data = Core.GuildData;
        if (data == null) return;

        await data.Save(guild.Id);
    }

    private Task GuildUnavailable(SocketGuild guild)
    {
        if (guild == null) return Task.FromResult(false);

        GuildData? data = Core.GuildData;
        if (data == null) return Task.FromResult(false);

        data.DeleteCache(guild.Id);
        return Task.CompletedTask;
    }
}