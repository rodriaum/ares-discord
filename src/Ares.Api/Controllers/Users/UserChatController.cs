using Ares.Common.DTOs;
using Ares.Common.Manager;
using Ares.Common.Models.Chat;
using Ares.Common.Models.Chat.Historic;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Api.Repository;
using Ares.Common.Util;
using Microsoft.AspNetCore.Mvc;

namespace Ares.Api.Controllers.Users;

[ApiController]
[Route("api/users/{userId}/chat")]
public class UserChatController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly UserDataManager _userDataManager;

    public UserChatController(UserRepository userRepository, UserDataManager userDataManager)
    {
        _userRepository = userRepository;
        _userDataManager = userDataManager;
    }

    [HttpPost("save-data")]
    public async Task<ActionResult<ApiResult<object>>> SaveChatData(ulong userId, [FromBody] UserChat chat)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.SaveChatDataAsync(user, chat);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to save chat data"));

            return Ok(ApiResult<object>.Ok(null, "Chat data saved successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error saving chat data for user {userId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpPost("guilds/{guildId}/create")]
    public async Task<ActionResult<ApiResult<object>>> CreateChatData(ulong userId, ulong guildId, [FromBody] UserChatInfo info)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.CreateChatData(user, guildId, info);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to create chat data"));

            return Ok(ApiResult<object>.Ok(true, "Chat data created successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error creating chat data for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/info")]
    public async Task<ActionResult<ApiResult<IEnumerable<UserChatInfo>>>> GetChatInfos(ulong userId, ulong guildId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<IEnumerable<UserChatInfo>>.Fail($"User with ID {userId} not found"));

            List<UserChatInfo>? chatInfos = _userDataManager.ChatInfos(user, guildId);

            if (chatInfos == null)
                return Ok(ApiResult<IEnumerable<UserChatInfo>>.Ok(new List<UserChatInfo>(), "Chat infos is null or empty, returned a empty list."));

            return Ok(ApiResult<IEnumerable<UserChatInfo>>.Ok(chatInfos));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting chat infos for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<UserChatInfo>>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/channels/{channelId}/info")]
    public async Task<ActionResult<ApiResult<UserChatInfo>>> GetChatInfoByChannel(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<UserChatInfo>.Fail($"User with ID {userId} not found"));

            var chatInfo = _userDataManager.ChatInfoByChannel(user, guildId, channelId);

            if (chatInfo == null)
                return NotFound(ApiResult<UserChatInfo>.Fail($"No chat info found for channel {channelId}"));

            return Ok(ApiResult<UserChatInfo>.Ok(chatInfo));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting chat info for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<UserChatInfo>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/history")]
    public async Task<ActionResult<ApiResult<IEnumerable<UserChatHistoric>>>> GetChatHistory(ulong userId, ulong guildId, [FromQuery] ulong channelId = 0)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<IEnumerable<UserChatHistoric>>.Fail($"User with ID {userId} not found"));

            var history = _userDataManager.ChatHistorics(user, guildId, channelId);

            if (history == null)
                return Ok(ApiResult<IEnumerable<UserChatHistoric>>.Ok(new List<UserChatHistoric>(), "No chat history found."));

            return Ok(ApiResult<IEnumerable<UserChatHistoric>>.Ok(history));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting chat history for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<UserChatHistoric>>.Fail("Internal server error"));
        }
    }

    [HttpPut("guilds/{guildId}/info")]
    public async Task<ActionResult<ApiResult<object>>> UpdateChatInfo(ulong userId, ulong guildId, [FromBody] UserChatInfo info)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.UpdateChatInfoAsync(user, guildId, info);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to update chat info"));

            return Ok(ApiResult<object>.Ok(null, "Chat info updated successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error updating chat info for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpPut("guilds/{guildId}/channels/{channelId}/toggle")]
    public async Task<ActionResult<ApiResult<object>>> ToggleChatStatus(ulong userId, ulong guildId, ulong channelId, [FromQuery] bool active)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.ToggleChatInfo(user, guildId, channelId, active);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to toggle chat status"));

            return Ok(ApiResult<object>.Ok(null, $"Chat status {(active ? "activated" : "deactivated")} successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error toggling chat status for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpPost("guilds/{guildId}/channels/{channelId}/history")]
    public async Task<ActionResult<ApiResult<bool>>> AddChatHistory(ulong userId, ulong guildId, ulong channelId, [FromBody] UserChatHistoric historic)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<bool>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.UpdateChatHistoricsAsync(user, guildId, channelId, historic);

            if (!success)
                return StatusCode(500, ApiResult<bool>.Fail("Failed to add chat history"));

            return Ok(ApiResult<bool>.Ok(true, "Chat history added successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error adding chat history for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<bool>.Fail("Internal server error"));
        }
    }

    [HttpPut("guilds/{guildId}/channels/{channelId}/history")]
    public async Task<ActionResult<ApiResult<object>>> UpdateChatHistory(ulong userId, ulong guildId, ulong channelId, [FromBody] List<UserChatHistoric> historics)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.UpdateChatHistoricsAsync(user, guildId, channelId, historics);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to update chat history"));

            return Ok(ApiResult<object>.Ok(true, "Chat history updated successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error updating chat history for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpDelete("guilds/{guildId}/channels/{channelId}/history")]
    public async Task<ActionResult<ApiResult<object>>> RemoveConversation(ulong userId, ulong guildId, ulong channelId, [FromBody] UserChatHistoric historic)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<object>.Fail($"User with ID {userId} not found"));

            var success = await _userDataManager.RemoveConversationAsync(user, guildId, channelId, historic);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to remove conversation"));

            return Ok(ApiResult<object>.Ok(true, "Conversation removed successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error removing conversation for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/active")]
    public async Task<ActionResult<ApiResult<bool>>> HasActiveConversation(ulong userId, ulong guildId, [FromQuery] ulong channelId = 0)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<bool>.Fail($"User with ID {userId} not found"));

            var hasActive = _userDataManager.HasActiveUserConversation(user, guildId, channelId);
            return Ok(ApiResult<bool>.Ok(hasActive));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error checking active conversation for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<bool>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/count")]
    public async Task<ActionResult<ApiResult<int>>> GetConversationCount(ulong userId, ulong guildId, [FromQuery] bool activeOnly = false)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<int>.Fail($"User with ID {userId} not found"));

            var count = _userDataManager.GetConversationsCount(user, guildId, activeOnly);
            return Ok(ApiResult<int>.Ok(count));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting conversation count for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<int>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/last-model")]
    public async Task<ActionResult<ApiResult<ChatModel>>> GetLastModel(ulong userId, ulong guildId, [FromQuery] ulong channelId = 0)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<ChatModel>.Fail($"User with ID {userId} not found"));

            var model = await _userDataManager.GetLastModelByUser(user, guildId, channelId);

            if (model == null)
                return NotFound(ApiResult<ChatModel>.Fail("No model found for user"));

            return Ok(ApiResult<ChatModel>.Ok(model));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting last model for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<ChatModel>.Fail("Internal server error"));
        }
    }

    [HttpGet("guilds/{guildId}/channels/{channelId}/is-owner")]
    public async Task<ActionResult<ApiResult<bool>>> IsChatOwner(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
                return NotFound(ApiResult<bool>.Fail($"User with ID {userId} not found"));

            var isOwner = _userDataManager.IsChatOwner(user, guildId, channelId);
            return Ok(ApiResult<bool>.Ok(isOwner));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error checking chat ownership for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<bool>.Fail("Internal server error"));
        }
    }
}