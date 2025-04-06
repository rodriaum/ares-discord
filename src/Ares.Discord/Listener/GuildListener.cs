/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Database.Collection;
using Ares.Core.Database.Model;
using Ares.Core.Util;
using Discord.WebSocket;

namespace Ares.Discord.Listener;

class GuildListener
{
    private static DiscordSocketClient? _client;

    public GuildListener(DiscordSocketClient client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client), "Client cannot be null");

        _client = client;

        _client.GuildAvailable += GuildAvailable;
        _client.GuildUnavailable += GuildUnavailable;
    }

    private async Task GuildAvailable(SocketGuild sguild)
    {
        if (sguild == null) return;

        GuildCollection? data = AresCore.GuildCollection;
        if (data == null) return;

        await AresLogger.LogAsync(nameof(GuildAvailable), $"Searching and caching guild \"{sguild.Id}\" in Redis.");

        Guild? guild = await data.FetchAsync(sguild.Id, saveInRedis: true);
        if (guild != null) return;

        await AresLogger.LogAsync(nameof(GuildAvailable), $"New guild \"{sguild.Id}\" found, it will be saved in the database.");

        await data.SaveAsync(sguild.Id);
    }

    private async Task GuildUnavailable(SocketGuild guild)
    {
        if (guild == null) return;

        GuildCollection? data = AresCore.GuildCollection;
        if (data == null) return;

        await AresLogger.LogAsync(nameof(GuildUnavailable), $"Guild \"{guild.Id}\" is not available, cache will be deleted.");
        await data.DeleteCache(guild.Id);
    }
}