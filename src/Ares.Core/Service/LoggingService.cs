/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Ares.Core.Util;

namespace Ares.Core.Service;

internal class LoggingService
{
    public LoggingService(DiscordSocketClient? client = null, CommandService? command = null)
    {
        if (client != null)
            client.Log += LogAsync;

        if (command != null)
            command.Log += LogAsync;
    }

    private Task LogAsync(LogMessage message)
    {
        if (message.Exception is CommandException ex)
        {
            AresLogger.Error(
                $"Command: {message.Source}", 
                $"{ex.Command.Aliases.First()} failed to execute in {ex.Context.Channel}.",
                ex.Message,
                severity: message.Severity
                );
        }
        else
        {
            AresLogger.Error($"General: {message.Source}", message.Message, severity: message.Severity);
        }

        return Task.CompletedTask;
    }
}