using Ares.Common.Manager;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Objects;
using Ares.Common.Repository;
using Ares.Common.Util;
using Microsoft.AspNetCore.Mvc;

namespace Ares.Api.Controllers.Users;

[ApiController]
[Route("api/users/{userId}/snippets")]
public class UserSnippetsController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly UserDataManager _userDataManager;

    public UserSnippetsController(UserRepository userRepository, UserDataManager userDataManager)
    {
        _userRepository = userRepository;
        _userDataManager = userDataManager;
    }

    /// <summary>
    /// Gets all snippets for a user in a specific guild
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <returns>List of snippets</returns>
    [HttpGet("guilds/{guildId}")]
    public async Task<ActionResult<IEnumerable<UserChatSnippet>>> GetSnippetsByGuild(ulong userId, ulong guildId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var snippets = _userDataManager.GetSnippetsByGuild(user, guildId);

            if (snippets == null)
            {
                return Ok(new List<UserChatSnippet>());
            }

            return Ok(snippets);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error getting snippets for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets snippets for a user in a specific guild and channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>List of snippets filtered by channel</returns>
    [HttpGet("guilds/{guildId}/channels/{channelId}")]
    public async Task<ActionResult<IEnumerable<UserChatSnippet>>> GetSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var snippets = _userDataManager.GetSnippetsByChannel(user, guildId, channelId);

            if (snippets == null)
            {
                return Ok(new List<UserChatSnippet>());
            }

            return Ok(snippets);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error getting snippets for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("guilds/{guildId}")]
    public async Task<IActionResult> AddSnippet(ulong userId, ulong guildId, [FromBody] UserChatSnippet snippet)
    {
        var user = await _userRepository.FetchAsync(userId);
        if (user == null)
            return NotFound(new { message = $"User with ID {userId} not found" });

        var result = await _userDataManager.SaveSnippetAsync(user, guildId, snippet);
        if (!result)
            return StatusCode(500, new { message = "Erro ao salvar snippet" });

        return Ok(snippet);
    }

    [HttpPost("guilds/{guildId}/batch")]
    public async Task<IActionResult> AddSnippetsBatch(ulong userId, ulong guildId, [FromBody] List<UserChatSnippet> snippets)
    {
        var user = await _userRepository.FetchAsync(userId);
        if (user == null)
            return NotFound(new { message = $"User with ID {userId} not found" });

        var result = await _userDataManager.SaveSnippetsAsync(user, guildId, snippets);
        if (!result)
            return StatusCode(500, new { message = "Erro ao salvar snippets" });

        return Ok(snippets);
    }

    [HttpDelete("guilds/{guildId}/channels/{channelId}")]
    public async Task<IActionResult> RemoveSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
    {
        var user = await _userRepository.FetchAsync(userId);
        if (user == null)
            return NotFound(new { message = $"User with ID {userId} not found" });

        var result = await _userDataManager.RemoveSnippetByChannelAsync(user, guildId, channelId);
        if (!result)
            return StatusCode(500, new { message = "Erro ao remover snippets" });

        return NoContent();
    }

    [HttpGet("guilds/{guildId}/snippets/{snippetId}")]
    public async Task<IActionResult> GetSnippetById(ulong userId, ulong guildId, string snippetId)
    {
        var user = await _userRepository.FetchAsync(userId);
        if (user == null)
            return NotFound(new { message = $"User with ID {userId} not found" });

        var snippet = _userDataManager.GetSnippetById(user, guildId, snippetId);
        if (snippet == null)
            return NotFound(new { message = "Snippet não encontrado" });

        return Ok(snippet);
    }

    [HttpGet("guilds/{guildId}/channels/{channelId}/index/{index}")]
    public async Task<IActionResult> GetSnippetByIndex(ulong userId, ulong guildId, ulong channelId, uint index)
    {
        var user = await _userRepository.FetchAsync(userId);
        if (user == null)
            return NotFound(new { message = $"User with ID {userId} not found" });

        var snippet = _userDataManager.GetSnippetByIndex(user, guildId, channelId, index);
        if (snippet == null)
            return NotFound(new { message = "Snippet não encontrado" });

        return Ok(snippet);
    }
}