using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Ares.src.Utils.Extra;

namespace Ares.src.Service
{
    internal class LoggingService
    {
        public LoggingService(DiscordSocketClient client, CommandService command)
        {
            client.Log += LogAsync;
            command.Log += LogAsync;
        }

        private Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException ex)
            {
                LogUtil.Error(
                    $"Command: {message.Severity}", 
                    $"{ex.Command.Aliases.First()} failed to execute in {ex.Context.Channel}.",
                    ex.Message
                    );
            }
            else
            {
                LogUtil.Error($"[General: {message.Severity}", "Failed to execute a command.", message.Message);
            }

            return Task.CompletedTask;
        }
    }
}
