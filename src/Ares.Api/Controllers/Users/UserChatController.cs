using Ares.Core.Manager;
using Ares.Core.Models.Chat;
using Ares.Core.Models.Chat.Historic;
using Ares.Core.Models.Data;
using Ares.Core.Objects;
using Ares.Core.Repository;
using Ares.Core.Util;
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

    /// <summary>
    /// Saves chat data for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="chat">Chat data to save</param>
    /// <returns>Success or error response</returns>
    [HttpPost("save-data")]
    public async Task<ActionResult> SaveChatData(ulong userId, [FromBody] UserChat chat)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.SaveChatDataAsync(user, chat);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to save chat data" });
            }

            return Ok(new { message = "Chat data saved successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error saving chat data for user {userId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates new chat data for a user in a guild
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="info">Chat information to create</param>
    /// <returns>Success or error response</returns>
    [HttpPost("guilds/{guildId}/create")]
    public async Task<ActionResult> CreateChatData(ulong userId, ulong guildId, [FromBody] UserChatInfo info)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.CreateChatData(user, guildId, info);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to create chat data" });
            }

            return Ok(new { message = "Chat data created successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error creating chat data for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets chat information for a user in a specific guild
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <returns>List of chat information</returns>
    [HttpGet("guilds/{guildId}/info")]
    public async Task<ActionResult<IEnumerable<UserChatInfo>>> GetChatInfos(ulong userId, ulong guildId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            List<UserChatInfo>? chatInfos = _userDataManager.ChatInfos(user, guildId);

            if (chatInfos == null)
            {
                return Ok(new List<UserChatInfo>());
            }

            return Ok(chatInfos);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting chat infos for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets chat information for a user in a specific channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>Chat information for the channel</returns>
    [HttpGet("guilds/{guildId}/channels/{channelId}/info")]
    public async Task<ActionResult<UserChatInfo>> GetChatInfoByChannel(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var chatInfo = _userDataManager.ChatInfoByChannel(user, guildId, channelId);

            if (chatInfo == null)
            {
                return NotFound(new { message = $"No chat info found for channel {channelId}" });
            }

            return Ok(chatInfo);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting chat info for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets chat history for a user in a guild, optionally filtered by channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Optional channel ID filter</param>
    /// <returns>List of chat history records</returns>
    [HttpGet("guilds/{guildId}/history")]
    public async Task<ActionResult<IEnumerable<UserChatHistoric>>> GetChatHistory(ulong userId, ulong guildId, [FromQuery] ulong channelId = 0)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var history = _userDataManager.ChatHistorics(user, guildId, channelId);

            if (history == null)
            {
                return Ok(new List<UserChatHistoric>());
            }

            return Ok(history);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting chat history for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates chat information for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="info">Updated chat information</param>
    /// <returns>Success or error response</returns>
    [HttpPut("guilds/{guildId}/info")]
    public async Task<ActionResult> UpdateChatInfo(ulong userId, ulong guildId, [FromBody] UserChatInfo info)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.UpdateChatInfoAsync(user, guildId, info);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to update chat info" });
            }

            return Ok(new { message = "Chat info updated successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error updating chat info for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Toggles chat active status for a user in a specific channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="active">New active status</param>
    /// <returns>Success or error response</returns>
    [HttpPut("guilds/{guildId}/channels/{channelId}/toggle")]
    public async Task<ActionResult> ToggleChatStatus(ulong userId, ulong guildId, ulong channelId, [FromQuery] bool active)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.ToggleChatInfo(user, guildId, channelId, active);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to toggle chat status" });
            }

            return Ok(new { message = $"Chat status {(active ? "activated" : "deactivated")} successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error toggling chat status for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a chat history record for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="historic">Chat history record to add</param>
    /// <returns>Success or error response</returns>
    [HttpPost("guilds/{guildId}/channels/{channelId}/history")]
    public async Task<ActionResult> AddChatHistory(ulong userId, ulong guildId, ulong channelId, [FromBody] UserChatHistoric historic)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.UpdateChatHistoricsAsync(user, guildId, channelId, historic);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to add chat history" });
            }

            return Ok(new { message = "Chat history added successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error adding chat history for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates chat history records for a user and channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="historics">Updated list of chat history records</param>
    /// <returns>Success or error response</returns>
    [HttpPut("guilds/{guildId}/channels/{channelId}/history")]
    public async Task<ActionResult> UpdateChatHistory(ulong userId, ulong guildId, ulong channelId, [FromBody] List<UserChatHistoric> historics)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.UpdateChatHistoricsAsync(user, guildId, channelId, historics);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to update chat history" });
            }

            return Ok(new { message = "Chat history updated successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error updating chat history for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Removes a specific conversation history record
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="historic">History record to remove</param>
    /// <returns>Success or error response</returns>
    [HttpDelete("guilds/{guildId}/channels/{channelId}/history")]
    public async Task<ActionResult> RemoveConversation(ulong userId, ulong guildId, ulong channelId, [FromBody] UserChatHistoric historic)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var success = await _userDataManager.RemoveConversationAsync(user, guildId, channelId, historic);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to remove conversation" });
            }

            return Ok(new { message = "Conversation removed successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error removing conversation for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Checks if user has active conversation
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Optional channel ID</param>
    /// <returns>Active status</returns>
    [HttpGet("guilds/{guildId}/active")]
    public async Task<ActionResult<bool>> HasActiveConversation(ulong userId, ulong guildId, [FromQuery] ulong channelId = 0)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var hasActive = _userDataManager.HasActiveUserConversation(user, guildId, channelId);
            return Ok(new { hasActiveConversation = hasActive });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error checking active conversation for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets conversation count for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="activeOnly">Filter by active conversations only</param>
    /// <returns>Conversation count</returns>
    [HttpGet("guilds/{guildId}/count")]
    public async Task<ActionResult<int>> GetConversationCount(ulong userId, ulong guildId, [FromQuery] bool activeOnly = false)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var count = _userDataManager.GetConversationsCount(user, guildId, activeOnly);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting conversation count for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the last chat model used by user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Optional channel ID</param>
    /// <returns>Chat model information</returns>
    [HttpGet("guilds/{guildId}/last-model")]
    public async Task<ActionResult<ChatModel>> GetLastModel(ulong userId, ulong guildId, [FromQuery] ulong channelId = 0)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var model = await _userDataManager.GetLastModelByUser(user, guildId, channelId);

            if (model == null)
            {
                return NotFound(new { message = "No model found for user" });
            }

            return Ok(model);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error getting last model for user {userId} in guild {guildId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Checks if user is owner of a chat in specific channel
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>Owner status</returns>
    [HttpGet("guilds/{guildId}/channels/{channelId}/is-owner")]
    public async Task<ActionResult<bool>> IsChatOwner(ulong userId, ulong guildId, ulong channelId)
    {
        try
        {
            var user = await _userRepository.FetchAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {userId} not found" });
            }

            var isOwner = _userDataManager.IsChatOwner(user, guildId, channelId);
            return Ok(new { isOwner });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UserChatController", $"Error checking chat ownership for user {userId} in guild {guildId} channel {channelId}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}