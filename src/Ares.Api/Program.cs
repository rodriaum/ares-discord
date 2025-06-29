using Ares.Common.Util;
using DotNetEnv;
using Serilog;

namespace Ares.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        bool isDocker = DockerUtil.IsRunningInDocker();

        Log.Information($"Is running in Docker: {isDocker ? "Yes" : "No"}");

        if (!isDocker)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            Log.Information($"Trying to load .env file from path: {envPath}");

            if (File.Exists(envPath))
            {
                Log.Information(".env file found!");
                Env.Load(envPath);
                Log.Information(".env file loaded successfully!");
            }
            else
            {
                Log.Warning(".env file not found in bin directory!");

                string rootPath = Path.Combine(baseDirectory, "..", "..", "..", "..", ".env");
                Log.Warning($"Trying to load from root directory: {rootPath}");

                if (File.Exists(rootPath))
                {
                    Log.Information(".env file found in root directory!");
                    Env.Load(rootPath);
                    Log.Information(".env file loaded successfully!");
                }
                else
                {
                    Log.Error("ERROR: .env file not found in any location!");
                    Log.Warning("Please create a .env file in the project root");
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
            .UseSerilog()
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