namespace Ares.src.Utils.Extra;

internal class LogUtil
{
    /* Variables */

    private static DateTime time = DateTime.UtcNow;

    /* Methods */

    public static void Log(string prefix, string message)
    {
        Console.WriteLine(Output(prefix, message));
    }

    public static void Error(string prefix, string message, string error = "")
    {
        Console.Error.WriteLine("\n" + Output(prefix, message, error) + "\n");
    }

    public static async Task LogAsync(string prefix, string message)
    {
        await Console.Out.WriteLineAsync(Output(prefix, message));
    }

    public static async Task ErrorAsync(string prefix, string message, string error = "")
    {
        await Console.Error.WriteLineAsync("\n" + Output(prefix, message, error) + "\n");
    }

    /* Helpers */

    private static string Output(string prefix, string message, string error = "")
    {
        bool isError = !string.IsNullOrEmpty(error);
        string date = $"{time.Hour}:{time.Minute}:{time.Second}";

        return $"[{date} - {(isError ? "Error" : "Info")}] {(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : "")}{message}{(isError ? $"\n -> {error}\n" : "")}";
    }
}