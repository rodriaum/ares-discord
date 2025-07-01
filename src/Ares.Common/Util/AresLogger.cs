/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using Ares.Common.Objects;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Text;

namespace Ares.Common.Util;

public class AresLogger
{
    private static DateTime time = DateTime.UtcNow;

    private static bool UseSerilog = false;

    #region Public Methods

    /// <summary>
    /// Initializes the Serilog logger with a console sink using the Literate theme.
    /// </summary>
    public static void BuildSerilog()
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateBootstrapLogger();

        UseSerilog = true;
    }

    /// <summary>
    /// Logs a message with embedded ANSI color codes.
    /// </summary>
    public static void LogWithColor(string message, Severity severity = Severity.Info)
    {
        if (UseSerilog)
        {
            // For Serilog, we pass the message with raw ANSI codes.
            // The {Message:l} in the output template will render them correctly.
            Serilog.Log.Write(ToSerilogLevel(severity), message);
        }
        else
        {
            // The existing logic doesn't support embedded colors, so we just log it.
            // A more complex implementation would be needed to parse the string.
            Log(null, message, severity);
        }
    }

    /// <summary>
    /// Logs a message with the specified prefix, message, severity, and optional extra information.
    /// </summary>
    public static void Log(string? prefix, string message, Severity severity = Severity.Info, params string[] extra)
    {
        if (UseSerilog)
        {
            LogEventLevel level = ToSerilogLevel(severity);
            StringBuilder logMessage = new StringBuilder();

            if (!string.IsNullOrEmpty(prefix))
            {
                logMessage.Append($"[{prefix}] ");
            }

            logMessage.Append(message);

            if (extra.Any())
            {
                logMessage.AppendLine();
                foreach (var item in extra)
                {
                    logMessage.AppendLine($" -> {item}");
                }
            }

            Serilog.Log.Write(level, logMessage.ToString());
        }
        else
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
    }

    /// <summary>
    /// Asynchronously logs a message with the specified prefix, message, severity, and optional extra information.
    /// </summary>]
    public static Task LogAsync(string prefix, string message, Severity severity = Severity.Info, params string[] extra)
    {
        Log(prefix, message, severity, extra);
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private static LogEventLevel ToSerilogLevel(Severity severity)
    {
        return severity switch
        {
            Severity.Critical => LogEventLevel.Fatal,
            Severity.Error => LogEventLevel.Error,
            Severity.Warning => LogEventLevel.Warning,
            Severity.Info => LogEventLevel.Information,
            Severity.Verbose => LogEventLevel.Verbose,
            Severity.Debug => LogEventLevel.Debug,
            Severity.Success => LogEventLevel.Information,
            _ => LogEventLevel.Information
        };
    }

    private static ConsoleColor GetColorBySeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Critical => ConsoleColor.DarkRed,
            Severity.Error => ConsoleColor.Red,
            Severity.Warning => ConsoleColor.Yellow,
            Severity.Info => ConsoleColor.White,
            Severity.Verbose => ConsoleColor.Gray,
            Severity.Debug => ConsoleColor.DarkGray,
            Severity.Success => ConsoleColor.Green,
            _ => ConsoleColor.White
        };
    }

    private static string Output(string? prefix, string message, Severity severity = Severity.Info, params string[] extra)
    {
        string date = $"{time.Hour}:{time.Minute}:{time.Second}";

        return $"[{date} - {severity.ToString()}] {(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : "")}{message}";
    }

    #endregion
}