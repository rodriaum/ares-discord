using Ares.src.Backend.Data;
using Ares.src.Guild.Config;
using Ares.src.Guild.Information;
using Ares.src.Guild.Token;
using Discord;
using Discord.WebSocket;

namespace Ares.src.Commands.Data;

internal class ConfigCommand
{
    private readonly DiscordSocketClient? _client;

    public ConfigCommand(DiscordSocketClient client)
    {
        client.SlashCommandExecuted += SlashCommandHandler;
        this._client = client;
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (this._client == null || !(command.Data.Name.Equals("config-token") || command.Data.Name.Equals("config-id") || command.Data.Name.Equals("config-lang"))) return;

        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Configuração")
            .WithDescription("Aguarde...")
            .WithColor(Color.Gold)
            .WithFooter($"{DateTime.Now.Year} | Ares");

        await command.RespondAsync(ephemeral: true, embed: embed.Build());
        Discord.Rest.RestInteractionMessage message = await command.GetOriginalResponseAsync();

        GuildData? data = Core.GuildData;
        if (data == null)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription("Não foi possível acessar as informações do servidor atual.")
                    .WithColor(Color.Red)
                    .Build()
            );
            return;
        }

        ulong? guildId = command.GuildId;

        if (guildId == null)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription("Não foi possível encontrar o ID do servidor atual.")
                    .WithColor(Color.Red)
                    .Build()
            );
            return;
        }

        Guild.Guild? guild = await data.Fetch(guildId.Value);
        const int maxAttempts = 3;

        for (int attempts = maxAttempts; guild == null && attempts > 0; attempts--)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription($"Servidor não foi encontrado no banco de dados! A criar... ({attempts}/{maxAttempts})")
                    .WithColor(Color.Red)
                    .Build());

            await Task.Delay(1500);
            guild = await data.Save(guildId.Value);
        }

        if (guild == null)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription("Não foi possível criar esse servidor no banco de dados! Tente novamente mais tarde.")
                    .WithColor(Color.Red)
                    .Build());
            return;
        }

        SocketSlashCommandDataOption? option = command.Data.Options.FirstOrDefault();

        if (option == null)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription("Não foi possível encontrar as opções. Tente novamente!")
                    .WithColor(Color.Red)
                    .Build()
            );
            return;
        }

        string optionName = option.Name;
        string? optionValue = option.Value.ToString();

        if (optionValue == null)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription("Não foi possível encontrar o valor na opção. Tente novamente!")
                    .WithColor(Color.Red)
                    .Build()
            );
            return;
        }

        GuildInformation information = guild.Information;

        GuildTokenData tokenData = information.Token;
        GuildConfigData configData = information.Config;

        bool tokenChange = false, configChange = false;

        switch (optionName)
        {

            /*
             * Token Configuration
             */

            case "openai":
                tokenData.OpenAi = optionValue;
                // Só salva se o token for diferente. Não há mensagem para evitar força bruta.
                tokenChange = tokenData.OpenAi != optionValue;
                break;

            case "anthropic":
                tokenData.Anthropic = optionValue;
                // Só salva se o token for diferente. Não há mensagem para evitar força bruta.
                tokenChange = tokenData.Anthropic != optionValue;
                break;

            case "deepseek":
                tokenData.Deepseek = optionValue;
                // Só salva se o token for diferente. Não há mensagem para evitar força bruta.
                tokenChange = tokenData.Deepseek != optionValue;
                break;

            case "imgur":
                tokenData.Imgur = optionValue;
                // Só salva se o token for diferente. Não há mensagem para evitar força bruta.
                tokenChange = tokenData.Imgur != optionValue;
                break;

            /*
             * IDs Configuration
             */

            case "role-member":
                configData.MemberRoleId = ulong.Parse(optionValue);
                configChange = true;
                break;

            case "role-usage":
                configData.UsageRoleId = ulong.Parse(optionValue);
                configChange = true;
                break;

            case "channel-setup":
                configData.SetupChannelId = ulong.Parse(optionValue);
                configChange = true;
                break;

            case "channel-log":
                configData.LogChannelId = ulong.Parse(optionValue);
                configChange = true;
                break;

            case "category-chats":
                configData.ChatsCategoryId = ulong.Parse(optionValue);
                configChange = true;
                break;

            /*
             * Lang Configuration
             */

            case "lang":
                configData.Lang = optionValue;
                configChange = true;
                break;

            /*
             * Default Option
             */

            default:
                await message.ModifyAsync(msg =>
                    msg.Embed = embed
                        .WithDescription("Não foi possível achar uma opção válida. Tente novamente!")
                        .WithColor(Color.Red)
                        .Build()
                );
                return;
        }

        // Só uma opção pode ser alterada por comando, por isso o uso de uma variável booleana apenas.
        bool success = false;

        if (tokenChange)
        {
            success = await guild.SaveGuildTokenDataAsync(tokenData);
        }

        if (configChange)
        {
            success = await guild.SaveGuildConfigDataAsync(configData);
        }

        if (success)
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription($"Sucesso! Opção **{optionName}** alterado para ||{optionValue}||")
                    .WithColor(Color.Green)
                    .Build()
            );
        }
        else
        {
            await message.ModifyAsync(msg =>
                msg.Embed = embed
                    .WithDescription($"Não foi possível alterar a opção **{optionName}** para ||{optionValue}||. Tente novamente!")
                    .WithColor(Color.Red)
                    .Build()
                );
        }
    }
}