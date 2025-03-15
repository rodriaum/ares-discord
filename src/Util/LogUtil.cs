using Discord;

namespace Ares.src.Util;

internal class LogUtil
{
    /* Variables */

    private static DateTime time = DateTime.UtcNow;

    /* Methods */

    public static void Log(string prefix, string message)
    {
        Console.WriteLine(Output(prefix, message));
    }

    public static async Task LogAsync(string prefix, string message)
    {
        await Console.Out.WriteLineAsync(Output(prefix, message));
    }

    public static void Error(string prefix, string message, string error = "", LogSeverity severity = LogSeverity.Error)
    {
        Console.Error.WriteLine("\n" + Output(prefix, message, error: error, severity: severity) + "\n");
    }

    public static async Task ErrorAsync(string prefix, string message, string error = "", LogSeverity severity = LogSeverity.Error)
    {
        await Console.Error.WriteLineAsync("\n" + Output(prefix, message, error: error, severity: severity) + "\n");
    }

    /* Helpers */

    private static string Output(string prefix, string message, string error = "", LogSeverity severity = LogSeverity.Info)
    {
        string date = $"{time.Hour}:{time.Minute}:{time.Second}";

        return $"[{date} - {severity.ToString()}] {(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : "")}{message}{(!string.IsNullOrEmpty(error) ? $"\n -> {error}\n" : "")}";
    }
}