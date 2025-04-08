using Discord;
using Discord.Rest;

namespace Ares.Ares.Core.Objects.Message;

/// <summary>
/// Helper class to handle message updates with rate limiting.
/// </summary>
public class MessageUpdater
{
    private readonly RestUserMessage? _message;
    private readonly EmbedBuilder _embed;
    private readonly TimeSpan _cooldown;
    private DateTime _lastEditDate = DateTime.UtcNow;

    public MessageUpdater(RestUserMessage? message, EmbedBuilder embed, TimeSpan cooldown)
    {
        _message = message;
        _embed = embed;
        _cooldown = cooldown;
    }

    public async Task UpdateMessageAsync(string text)
    {
        if (_message == null) return;

        // Apply text length limits for embeds
        string displayText = text;
        if (displayText.Length > 4096)
        {
            displayText = displayText.Substring(0, 4095);
            _embed.WithFooter($"{DateTime.Now.Year} - Ares | (♾️)");
        }

        _embed.WithDescription(displayText);

        // Update message if cooldown has passed
        if ((DateTime.UtcNow - _lastEditDate) > _cooldown)
        {
            await _message.ModifyAsync(message => message.Embed = _embed.Build());
            _lastEditDate = DateTime.UtcNow;
        }
    }
}
