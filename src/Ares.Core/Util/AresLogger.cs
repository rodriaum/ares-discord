/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Objects;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;

namespace Ares.Core.Util;

public class AresLogger
{
    private static DateTime time = DateTime.UtcNow;

    /* Functions */

    public static void Log(string prefix, string message, string extra = "", Severity severity = Severity.Info)
    {
        Console.ForegroundColor = GetColorBySeverity(severity);
        Console.Error.WriteLine(Output(prefix, message, extra: extra, severity: severity));
        Console.ResetColor();
    }

    public static async Task LogAsync(string prefix, string message, string extra = "", Severity severity = Severity.Info)
    {
        Console.ForegroundColor = GetColorBySeverity(severity);
        await Console.Error.WriteLineAsync(Output(prefix, message, extra: extra, severity: severity));
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
            _ => ConsoleColor.Gray
        };
    }

    private static string Output(string prefix, string message, string extra = "", Severity severity = Severity.Info)
    {
        string date = $"{time.Hour}:{time.Minute}:{time.Second}";

        return $"[{date} - {severity.ToString()}] {(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : "")}{message}{(!string.IsNullOrEmpty(extra) ? $"\n -> {extra}\n" : "")}";
    }
}