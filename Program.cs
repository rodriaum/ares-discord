using Discord;
using Discord.Net;
using Discord.WebSocket;
using Ares.commands;
using Ares.Commands;
using Ares.Listener.Chat;
using Ares.Manager;
using Ares.Util.Extra;
using Newtonsoft.Json;
using Ares.Listener.Chat.Button;
using OpenAI;
using DotNetEnv;

namespace Ares
{
    internal class Program
    {

        /* VARIABLES */

        public static DiscordSocketClient? Client { get; private set; }

        /* MAIN */

        static async Task Main()
        {
            Env.Load();
            Core.Init();
            
            string token = "MTI3ODQ0NzI3NzkwNzY0NDU3OA.G3M0OQ.g-cEiALVtPO4trpE6r2u2XjpmMR8LMGlkWD01o";

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            Client = new DiscordSocketClient(config);

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            // Listeners
            new SelectedChatListener(Client);
            new ButtonChatListener(Client);
            new ReceivedContentListener(Client);

            // Commands
            new PingCommand(Client);
            new SetupCommand(Client);

            // Managers
            new OpenAiManager().Init();

            // Options
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
            await Client.SetGameAsync("Feito com ❤️ pelo Rodrigo!", type:ActivityType.CustomStatus);

            Client.Ready += RegisterCommands;
            Client.Ready += () =>
            {
                SocketSelfUser selfUser = Client.CurrentUser;

                LogUtil.Log("READY", $"Sucess! Logged to \"{selfUser.Username}#{selfUser.Discriminator}\"");

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
    }
}