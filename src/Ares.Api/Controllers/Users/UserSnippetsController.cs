using Ares.Common.DTOs;
using Ares.Common.Manager;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Api.Repository;
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
    [HttpGet("guilds/{guildId}")]
    public async Task<ActionResult<ApiResult<IEnumerable<UserChatSnippet>>>> GetSnippetsByGuild(ulong userId, ulong guildId)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<IEnumerable<UserChatSnippet>>.Fail($"User with ID {userId} not found"));

            List<UserChatSnippet>? snippets = _userDataManager.GetSnippetsByGuild(user, guildId);

            if (snippets == null)
                return Ok(ApiResult<IEnumerable<UserChatSnippet>>.Ok(new List<UserChatSnippet>(), "Nenhum snippet encontrado."));

            return Ok(ApiResult<IEnumerable<UserChatSnippet>>.Ok(snippets));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error getting snippets for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<UserChatSnippet>>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Gets snippets for a user in a specific guild and channel
    /// </summary>
    [HttpGet("guilds/{guildId}/channels/{channelId}")]
    public async Task<ActionResult<ApiResult<IEnumerable<UserChatSnippet>>>> GetSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<IEnumerable<UserChatSnippet>>.Fail($"User with ID {userId} not found"));

            List<UserChatSnippet>? snippets = _userDataManager.GetSnippetsByChannel(user, guildId, channelId);

            if (snippets == null)
                return Ok(ApiResult<IEnumerable<UserChatSnippet>>.Ok(new List<UserChatSnippet>(), "Nenhum snippet encontrado."));

            return Ok(ApiResult<IEnumerable<UserChatSnippet>>.Ok(snippets));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error getting snippets for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<UserChatSnippet>>.Fail("Internal server error"));
        }
    }

    [HttpPost("guilds/{guildId}")]
    public async Task<ActionResult<ApiResult<UserChatSnippet>>> AddSnippet(ulong userId, ulong guildId, [FromBody] UserChatSnippet snippet)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<UserChatSnippet>.Fail($"User with ID {userId} not found"));

            bool result = await _userDataManager.SaveSnippetAsync(user, guildId, snippet);
            if (!result)
                return StatusCode(500, ApiResult<UserChatSnippet>.Fail("Erro ao salvar snippet"));

            return Ok(ApiResult<UserChatSnippet>.Ok(snippet, "Snippet salvo com sucesso"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error saving snippet for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<UserChatSnippet>.Fail("Internal server error"));
        }
    }

    [HttpPost("guilds/{guildId}/batch")]
    public async Task<ActionResult<ApiResult<IEnumerable<UserChatSnippet>>>> AddSnippetsBatch(ulong userId, ulong guildId, [FromBody] List<UserChatSnippet> snippets)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<IEnumerable<UserChatSnippet>>.Fail($"User with ID {userId} not found"));

            bool result = await _userDataManager.SaveSnippetsAsync(user, guildId, snippets);
            if (!result)
                return StatusCode(500, ApiResult<IEnumerable<UserChatSnippet>>.Fail("Erro ao salvar snippets"));

            return Ok(ApiResult<IEnumerable<UserChatSnippet>>.Ok(snippets, "Snippets salvos com sucesso"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error saving snippets for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<UserChatSnippet>>.Fail("Internal server error"));
        }
    }

    [HttpDelete("guilds/{guildId}/channels/{channelId}")]
    public async Task<ActionResult<ApiResult<object>>> RemoveSnippetsByChannel(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            bool result = await _userDataManager.RemoveSnippetByChannelAsync(user, guildId, channelId);
            if (!result)
                return StatusCode(500, ApiResult<object>.Fail("Erro ao remover snippets"));

            return Ok(ApiResult<object>.Ok(null, "Snippets removidos com sucesso"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error removing snippets for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/snippets/{snippetId}")]
    public async Task<ActionResult<ApiResult<UserChatSnippet>>> GetSnippetById(ulong userId, ulong guildId, string snippetId)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<UserChatSnippet>.Fail($"User with ID {userId} not found"));

            UserChatSnippet? snippet = _userDataManager.GetSnippetById(user, guildId, snippetId);
            if (snippet == null)
                return NotFound(ApiResult<UserChatSnippet>.Fail("Snippet não encontrado"));

            return Ok(ApiResult<UserChatSnippet>.Ok(snippet));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error getting snippet {snippetId} for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<UserChatSnippet>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/channels/{channelId}/index/{index}")]
    public async Task<ActionResult<ApiResult<UserChatSnippet>>> GetSnippetByIndex(ulong userId, ulong guildId, ulong channelId, uint index)
    {
        try
        {
            User? user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<UserChatSnippet>.Fail($"User with ID {userId} not found"));

            UserChatSnippet? snippet = _userDataManager.GetSnippetByIndex(user, guildId, channelId, index);
            if (snippet == null)
                return NotFound(ApiResult<UserChatSnippet>.Fail("Snippet não encontrado"));

            return Ok(ApiResult<UserChatSnippet>.Ok(snippet));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserSnippetsController", $"Error getting snippet by index for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<UserChatSnippet>.Fail("Internal server error"));
        }
    }
}