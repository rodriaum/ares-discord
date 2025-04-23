/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core;
using Ares.Core.Models;
using Ares.Core.Repository;
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

    private Task GuildAvailable(SocketGuild sguild)
    {
        _ = Task.Run(async () =>
        {
            if (sguild == null) return;

            GuildRepository? repository = AresCore.GRepository;
            if (repository == null) return;

            await AresLogger.LogAsync("DB", $"Searching in database and caching guild \"{sguild.Id}\" in Redis.");

            Guild? guild = await repository.FetchAsync(sguild.Id, saveInRedis: true);
            if (guild != null) return;

            await AresLogger.LogAsync("DB", $"New guild \"{sguild.Id}\" found, it will be saved in the database.");

            await repository.SaveAsync(sguild.Id);
        });

        return Task.CompletedTask;
    }

    private Task GuildUnavailable(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            if (guild == null) return;

            GuildRepository? repository = AresCore.GRepository;
            if (repository == null) return;

            await AresLogger.LogAsync("DB: Redis", $"Guild \"{guild.Id}\" is not available, cache will be deleted.");
            await repository.DeleteCache(guild.Id);
        });

        return Task.CompletedTask;
    }
}