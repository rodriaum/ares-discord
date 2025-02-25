using Discord;
using Discord.Net;
using Discord.WebSocket;
using Ares.src.Commands;
using Ares.src.Listener.Chat;
using Ares.src.Manager;
using Newtonsoft.Json;
using Ares.src.Listener.Chat.Button;
using DotNetEnv;
using Ares.src.Utils.Extra;
using Ares.src.Commands.System;
using Ares.src.Commands.Data;
using Ares.src.Listener;
namespace Ares.src
{
    internal class Program
    {

        /* Discord Variables */

        private static DiscordSocketClient? _client { get; set; }

        /* Main Procedures */

        static async Task Main()
        {
            Env.Load();

            Core.Init();

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            _client = new DiscordSocketClient(config);

            await _client.LoginAsync(TokenType.Bot, Env.GetString("DISCORD_TOKEN"));
            await _client.StartAsync();

            // Listeners
            new SelectedChatListener(_client);
            new ButtonChatListener(_client);
            new ReceivedContentListener(_client);
            new GuildListener(_client);

            // Commands
            new PingCommand(_client);
            new SetupCommand(_client);
            new TokenConfigCommand(_client);
            new ConfigCommand(_client);

            // Managers
            new AiManager();

            // Options
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            await _client.SetGameAsync("Feito com ❤️ pelo Rodrigo!", type: ActivityType.CustomStatus);

            _client.Ready += RegisterCommands;
            _client.Ready += () =>
            {
                LogUtil.Log("Status", $"Success! Logged \"{_client.CurrentUser.Username}\"");
                return Task.CompletedTask;
            };


            await Task.Delay(Timeout.Infinite);
        }

        /* Methods Async */

        public static async Task RegisterCommands()
        {
            List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder()
               .WithName("ping")
               .WithDescription("Ping do gateway atual"),

                new SlashCommandBuilder()
                .WithName("config-token")
                .WithDescription("Configure os tokens do servidor atual.")
                .AddOptions(new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.String,
                    Name = "openai",
                    Description = "Acesse: https://platform.openai.com/settings/organization/api-keys",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.String,
                    Name = "anthropic",
                    Description = "Acesse: https://console.anthropic.com/settings/keys",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.String,
                    Name = "deepseek",
                    Description = "Acesse: https://platform.deepseek.com/api_keys",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.String,
                    Name = "imgur",
                    Description = "Acesse: https://api.imgur.com/oauth2/addclient",
                    IsRequired = true
                }),

                new SlashCommandBuilder()
                .WithName("config-id")
                .WithDescription("Configure os canais do servidor atual.")
                .AddOptions(new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.Number,
                    Name = "role-member",
                    Description = "Insira o ID do cargo de membro.",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.Number,
                    Name = "role-usage",
                    Description = "Insira o ID do cargo que pode usar os chats.",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.Number,
                    Name = "channel-setup",
                    Description = "Insira o ID do canal a onde vai ficar a embed de chats.",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.Number,
                    Name = "channel-log",
                    Description = "Insira o ID do canal a onde vai ficar as logs do bot.",
                    IsRequired = true
                },
                new SlashCommandOptionBuilder
                {
                    Type = ApplicationCommandOptionType.Number,
                    Name = "category-chats",
                    Description = "Insira o ID da categoria a onde vai ficar os canais dos chats gerados.",
                    IsRequired = true
                }),

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
                        new ApplicationCommandOptionChoiceProperties { Name = "Geração AI", Value = "setup-ai-menu" }
                    }
                })
            };

            try
            {
                // Function executed only after successful connection. But avoid alerts.
                if (_client == null)
                    return;

                foreach (SlashCommandBuilder command in commands)
                {
                    var build = command.Build();

                    await _client.CreateGlobalApplicationCommandAsync(build);

                    LogUtil.Log("Commands", $"Command \"{build.Name}\" registered.");
                }

            }
            catch (HttpException e)
            {
                string json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);

                LogUtil.Log("Commands", "Unable to register commands.\n -> " + (!(string.IsNullOrEmpty(json) || json.Equals("[]")) ? json : e.Message));
            }
        }
    }
}