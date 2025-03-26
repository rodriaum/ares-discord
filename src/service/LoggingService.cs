using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Ares.src.Util;

namespace Ares.src.Service;

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
                $"Command", 
                $"{ex.Command.Aliases.First()} failed to execute in {ex.Context.Channel}.",
                ex.Message,
                severity: message.Severity
                );
        }
        else
        {
            AresLogger.Error($"General", message.Message, severity: message.Severity);
        }

        return Task.CompletedTask;
    }
}