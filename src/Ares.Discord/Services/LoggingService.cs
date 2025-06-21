/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Objects;
using Ares.Common.Util;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Ares.Discord.Service;

public class LoggingService
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
        Severity severity = Enum.GetValues(typeof(Severity))
            .Cast<Severity>()
            .FirstOrDefault(x => x.ToString().ToLower() == message.Severity.ToString().ToLower(), Severity.Error);

        if (message.Exception is CommandException ex)
        {
            AresLogger.Log(
					$"Command: {message.Source}",
					$"{ex.Command.Aliases.First()} failed to execute in {ex.Context.Channel}.",
					extra: ex.Message,
					severity: severity
                );
        }
        else
        {
            // Will be ignore info messages because the Discord.Net SDK has random logs.
            if (message.Severity != LogSeverity.Info)
            {
                Exception exception = message.Exception;

                AresLogger.Log
                    (
                        $"General: {message.Source}",
                        message.Message,
                        extra: (exception != null ? exception.Message : ""),
                        severity: severity
                    );
            }
        }

        return Task.CompletedTask;
    }
}