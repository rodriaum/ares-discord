using Discord.WebSocket;
using Discord;
using Discord.Net;
using Newtonsoft.Json;
using Discord_OpenAI.commands;

namespace Discord_OpenAI
{
    internal class Program
    {

        public static DiscordSocketClient? Client { get; private set; }

        static async Task Main()
        {
            string token = "MTI0OTE0MzQwMjAwODk0MDYwNQ.GD7gzM.odgXvJMIxT2EcljWyI-Wg5LDRf-7hA4hgKt2SI";

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            Client = new DiscordSocketClient(config);

            await Client.LoginAsync(TokenType.Bot, token);

            Client.MessageUpdated += MessageUpdated;
            await Client.StartAsync();

            new PingCommand(Client);

            Client.Ready += RegisterCommands;
            Client.Ready += () =>
            {
                SocketSelfUser selfUser = Client.CurrentUser;

                Console.WriteLine($"Sucess! Logged to {selfUser.Username}#{selfUser.Discriminator}");
                return Task.CompletedTask;
            };

            await Task.Delay(Timeout.Infinite);
        }

        public static async Task RegisterCommands()
        {
            List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>();

            commands.Add(new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Comando gostoso"));

            try
            {
                // Function executed only after successful connection. But avoid alerts.
                if (Client == null)
                    return;

                foreach (SlashCommandBuilder command in commands)
                {
                    var build = command.Build();
                    
                    await Client.CreateGlobalApplicationCommandAsync(build);

                    Console.WriteLine($"Command {build.Name} registered successfully.");
                }

            }
            catch (HttpException e)
            {
                string json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);

                Console.WriteLine("Unable to register commands.\n -> " + (!(string.IsNullOrEmpty(json) || json.Equals("[]")) ? json : e.Message));
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
