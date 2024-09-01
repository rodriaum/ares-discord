using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord_OpenAI.commands;
using Discord_OpenAI.Commands;
using Discord_OpenAI.Manager;
using Discord_OpenAI.Util.Extra;
using Newtonsoft.Json;

namespace Discord_OpenAI
{
    internal class Program
    {

        /* VARIABLES */

        public static DiscordSocketClient? Client { get; private set; }

        /* MAIN */

        static async Task Main()
        {
            Core.Init();

            string token = "MTI0OTE0MzQwMjAwODk0MDYwNQ.GD7gzM.odgXvJMIxT2EcljWyI-Wg5LDRf-7hA4hgKt2SI";

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            Client = new DiscordSocketClient(config);

            await Client.LoginAsync(TokenType.Bot, token);

            Client.MessageUpdated += MessageUpdated;
            await Client.StartAsync();

            // Commands
            new PingCommand(Client);
            new SetupCommand(Client);

            // Managers
            new OpenAiManager().Init();

            Client.Ready += RegisterCommands;
            Client.Ready += () =>
            {
                SocketSelfUser selfUser = Client.CurrentUser;

                LogUtil.Separator();
                LogUtil.Log("READY", $"Sucess! Logged to \"{selfUser.Username}#{selfUser.Discriminator}\"");
                LogUtil.Separator();

                return Task.CompletedTask;
            };

            await Task.Delay(Timeout.Infinite);
        }

        /* METHODS */

        public static async Task RegisterCommands()
        {
            List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder()
               .WithName("ping")
               .WithDescription("Ping do gateway atual"),

                new SlashCommandBuilder()
                .WithName("setup")
                .WithDescription("Escolha o tipo de setup que deseja realizar no canal atual.")
                .AddOptions(new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.String,
                    Name = "option",
                    Description = "Escolha o tipo de setup que deseja realizar no canal atual.",
                    IsRequired = true,
                    Choices = new List<ApplicationCommandOptionChoiceProperties>
                    {
                        new ApplicationCommandOptionChoiceProperties { Name = "OpenAI", Value = "setup-openai-menu" }
                    }
                })
            };

            LogUtil.Separator();

            try
            {
                // Function executed only after successful connection. But avoid alerts.
                if (Client == null)
                    return;

                foreach (SlashCommandBuilder command in commands)
                {
                    var build = command.Build();

                    await Client.CreateGlobalApplicationCommandAsync(build);

                    LogUtil.Log("COMMAND", $"Command \"{build.Name}\" registered successfully.");
                }

            }
            catch (HttpException e)
            {
                string json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);

                LogUtil.Log("COMMAND", "Unable to register commands.\n -> " + (!(string.IsNullOrEmpty(json) || json.Equals("[]")) ? json : e.Message));
            }
        }

        private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            // If the message was not in the cache, downloading it will result in getting a copy of `after`.
            var message = await before.GetOrDownloadAsync();
            Console.WriteLine($"{message} -> {after}");
        }
    }
}