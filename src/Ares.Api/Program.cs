using Ares.Common.Objects;
using Ares.Common.Util;
using DotNetEnv;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Ares.Api;

public class Program
{
    public static void Main(string[] args)
    {
        AresLogger.BuildSerilog();

        bool isDocker = DockerUtil.IsRunningInDocker();

        const string green = "\u001b[32m";
        const string lightRed = "\u001b[91m";
        const string aqua = "\u001b[38;2;0;255;255m";
        const string reset = "\u001b[0m";

        string dockerStatus = isDocker ? $"{green}Yes{reset}" : $"{lightRed}No{reset}";
        AresLogger.LogWithColor($"Is running in Docker: {dockerStatus}");

        if (!isDocker)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            AresLogger.LogWithColor($"Trying to load .env file from path: {aqua}{envPath}{reset}");

            if (File.Exists(envPath))
            {
                AresLogger.Log(null, ".env file found!");
                Env.Load(envPath);
                AresLogger.Log(null, ".env file loaded successfully!");
            }
            else
            {
                AresLogger.Log(null, ".env file not found in bin directory!", severity: Severity.Warning);

                string rootPath = Path.Combine(baseDirectory, "..", "..", "..", "..", "..", ".env");
                AresLogger.Log(null, $"Trying to load from root directory: {aqua}{rootPath}{reset}", severity: Severity.Warning);

                if (File.Exists(rootPath))
                {
                    AresLogger.Log(null, ".env file found in root directory!");
                    Env.Load(rootPath);
                    AresLogger.Log(null, ".env file loaded successfully!");
                }
                else
                {
                    AresLogger.Log(null, "ERROR: .env file not found in any location!", severity: Severity.Error);
                    AresLogger.Log(null, "Please create a .env file in the project root", severity: Severity.Warning);
                    Environment.Exit(1);
                }
            }
        }

        try
        {
            Log.Information("Starting web application");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    theme: new AnsiConsoleTheme(
                        new Dictionary<ConsoleThemeStyle, string>
                        {
                            [ConsoleThemeStyle.String] = "\u001b[36m",
                            [ConsoleThemeStyle.Text] = "\u001b[37m",
                            [ConsoleThemeStyle.SecondaryText] = "\u001b[90m",
                            [ConsoleThemeStyle.TertiaryText] = "\u001b[90m",
                            [ConsoleThemeStyle.Invalid] = "\u001b[91m",
                            [ConsoleThemeStyle.Null] = "\u001b[95m",
                            [ConsoleThemeStyle.Name] = "\u001b[90m",
                            [ConsoleThemeStyle.Number] = "\u001b[95m",
                            [ConsoleThemeStyle.Boolean] = "\u001b[95m",
                            [ConsoleThemeStyle.Scalar] = "\u001b[95m",
                            [ConsoleThemeStyle.LevelVerbose] = "\u001b[90m",
                            [ConsoleThemeStyle.LevelDebug] = "\u001b[90m",
                            [ConsoleThemeStyle.LevelInformation] = "\u001b[36m",
                            [ConsoleThemeStyle.LevelWarning] = "\u001b[93m",
                            [ConsoleThemeStyle.LevelError] = "\u001b[91m",
                            [ConsoleThemeStyle.LevelFatal] = "\u001b[91;1m"
                        })))
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.ConfigureKestrel(options =>
                {
                    options.Limits.MaxConcurrentConnections = 10000;
                    options.Limits.MaxConcurrentUpgradedConnections = 10000;
                    options.Limits.MaxRequestBodySize = 10 * 1024; // 10 KB
                    options.Limits.MinRequestBodyDataRate = null;
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                });
            });
}