/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.DTOs;
using Ares.Common.Models.Data;
using Ares.Common.Util;
using Ares.Discord.Services.Api;
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

            GuildService? guildService = Program.GuildService;
            if (guildService == null) return;

            await AresLogger.LogAsync("DB", $"Searching in database and caching guild \"{sguild.Id}\" in redis.");

            ApiResult<Guild>? guildResult = await guildService.GetGuild(sguild.Id, useCache: true);
            if (guildResult != null && guildResult.Success && guildResult.Data != null) return;

            await AresLogger.LogAsync("DB", $"New guild \"{sguild.Id}\" found, it will be saved in the database.");

            // There is no need to handle the return here as it is just to ensure creation/caching.
            await guildService.CreateOrGetGuild(sguild.Id);
        });

        return Task.CompletedTask;
    }

    private Task GuildUnavailable(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            if (guild == null) return;

            GuildService? guildService = Program.GuildService;
            if (guildService == null) return;

            await AresLogger.LogAsync("DB: Redis", $"Guild \"{guild.Id}\" is not available, cache will be deleted.");
            await guildService.DeleteGuildCache(guild.Id);
        });

        return Task.CompletedTask;
    }
}