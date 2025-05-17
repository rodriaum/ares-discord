/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects;

namespace Ares.Core.Util;

public class AresLogger
{
    private static DateTime time = DateTime.UtcNow;

    /* Functions */

    public static void Log(string prefix, string message, Severity severity = Severity.Info, params string[] extra)
    {
        Console.ForegroundColor = GetColorBySeverity(severity);

        Console.Error.WriteLine(Output(prefix, message, severity: severity, extra: extra));

        if (extra.Any())
        {
            for (int i = 0; i < extra.Length; i++)
            {
                Console.Error.WriteLine($" -> {extra[i]}");

                if (i > extra.Length - 1)
                    Console.Error.WriteLine("\n");
            }
        }

        Console.ResetColor();
    }

    public static async Task LogAsync(string prefix, string message, Severity severity = Severity.Info, params string[] extra)
    {
        Console.ForegroundColor = GetColorBySeverity(severity);

        await Console.Error.WriteLineAsync(Output(prefix, message, severity: severity, extra: extra));

        if (extra.Any())
        {
            for (int i = 0; i < extra.Length; i++)
            {
                await Console.Error.WriteLineAsync($" -> {extra[i]}");

                if (i > extra.Length - 1)
                    await Console.Error.WriteLineAsync("\n");
            }
        }

        Console.ResetColor();
    }

    /* Helpers */

    private static ConsoleColor GetColorBySeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Critical => ConsoleColor.DarkRed,
            Severity.Error => ConsoleColor.Red,
            Severity.Warning => ConsoleColor.Yellow,
            Severity.Info => ConsoleColor.Gray,
            Severity.Verbose => ConsoleColor.White,
            Severity.Debug => ConsoleColor.DarkGray,
            Severity.Success => ConsoleColor.Green,
            _ => ConsoleColor.Gray
        };
    }

    private static string Output(string prefix, string message, Severity severity = Severity.Info, params string[] extra)
    {
        string date = $"{time.Hour}:{time.Minute}:{time.Second}";

        return $"[{date} - {severity.ToString()}] {(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : "")}{message}";
    }
}