/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Core.Models;

namespace Ares.Core.Util;

public class AresLogger
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

    public static void Error(string prefix, string message, string extra = "", Severity severity = Severity.Error)
    {
        Console.Error.WriteLine("\n" + Output(prefix, message, extra: extra, severity: severity) + "\n");
    }

    public static async Task ErrorAsync(string prefix, string message, string extra = "", Severity severity = Severity.Error)
    {
        await Console.Error.WriteLineAsync("\n" + Output(prefix, message, extra: extra, severity: severity) + "\n");
    }

    /* Helpers */

    private static string Output(string prefix, string message, string extra = "", Severity severity = Severity.Info)
    {
        string date = $"{time.Hour}:{time.Minute}:{time.Second}";

        return $"[{date} - {severity.ToString()}] {(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : "")}{message}{(!string.IsNullOrEmpty(extra) ? $"\n -> {extra}\n" : "")}";
    }
}