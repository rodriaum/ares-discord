using Ares.Common.DTOs;
using Ares.Common.Manager;
using Ares.Common.Models.Data;
using Ares.Common.Models.Preference;
using Ares.Common.Models.Token;
using Ares.Common.Objects;
using Ares.Common.Repository;
using Ares.Common.Util;
using Microsoft.AspNetCore.Mvc;

namespace Ares.Api.Controllers.Guilds;

[ApiController]
[Route("api/guilds")]
public class GuildsController : ControllerBase
{
    private readonly GuildRepository _guildRepository;
    private readonly GuildDataManager _guildDataManager;

    public GuildsController(GuildRepository guildRepository, GuildDataManager guildDataManager)
    {
        _guildRepository = guildRepository;
        _guildDataManager = guildDataManager;
    }

    /// <summary>
    /// Creates or retrieves a guild by ID
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <returns>Guild object ou erro</returns>
    [HttpPost("{id}/create-or-get")]
    public async Task<ActionResult<ApiResult<Guild>>> CreateOrGetGuild(ulong id)
    {
        try
        {
            Guild? guild = await _guildRepository.SaveAsync(id);

            if (guild == null)
            {
                return StatusCode(500, ApiResult<Guild>.Fail("Failed to create or retrieve guild"));
            }

            return Ok(ApiResult<Guild>.Ok(guild));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error creating/getting guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<Guild>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Retrieves a guild by ID
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <param name="useCache">Whether to save in Redis cache if fetched from database</param>
    /// <returns>Guild object ou not found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResult<Guild>>> GetGuild(ulong id, [FromQuery] bool useCache = true)
    {
        try
        {
            Guild? guild = await _guildRepository.FetchAsync(id, saveInRedis: useCache);

            if (guild == null)
            {
                return NotFound(ApiResult<Guild>.Fail($"Guild with ID {id} not found"));
            }

            return Ok(ApiResult<Guild>.Ok(guild));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error fetching guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<Guild>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Updates a guild
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <param name="guild">Updated guild object</param>
    /// <param name="field">Field name being updated (for logging)</param>
    /// <returns>Success ou erro</returns>
    [HttpPut("{id}/update")]
    public async Task<ActionResult<ApiResult<object>>> UpdateGuild(ulong id, [FromBody] Guild guild, [FromQuery] string field = "data")
    {
        try
        {
            if (guild.Id != id)
            {
                return BadRequest(ApiResult<object>.Fail("Guild ID in URL doesn't match guild object ID"));
            }

            bool success = await _guildRepository.UpdateAsync(guild, field);

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail("Failed to update guild"));
            }

            return Ok(ApiResult<object>.Ok("Guild updated successfully"));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error updating guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Saves token data for a guild
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <param name="token">Token data</param>
    /// <returns>Success ou erro</returns>
    [HttpPut("{id}/token")]
    public async Task<ActionResult<ApiResult<object>>> SaveTokenData(ulong id, [FromBody] GToken token)
    {
        try
        {
            Guild? guild = await _guildRepository.FetchAsync(id);

            if (guild == null)
            {
                return NotFound(ApiResult<object>.Fail($"Guild with ID {id} not found"));
            }

            bool success = await _guildDataManager.SaveTokenDataAsync(guild, token);

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail("Failed to save token data"));
            }

            return Ok(ApiResult<object>.Ok(null, "Token data saved successfully"));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error saving token data for guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Saves preference data for a guild
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <param name="preferences">Preference data</param>
    /// <returns>Success ou erro</returns>
    [HttpPut("{id}/preferences")]
    public async Task<ActionResult<ApiResult<object>>> SavePreferenceData(ulong id, [FromBody] GPreference preferences)
    {
        try
        {
            Guild? guild = await _guildRepository.FetchAsync(id);

            if (guild == null)
            {
                return NotFound(ApiResult<object>.Fail($"Guild with ID {id} not found"));
            }

            bool success = await _guildDataManager.SavePreferenceDataAsync(guild, preferences);

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail("Failed to save preferences"));
            }

            return Ok(ApiResult<object>.Ok(null, "Preferences saved successfully"));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error saving preferences for guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Retrieves all guilds with optional limit
    /// </summary>
    /// <param name="limit">Maximum number of guilds to retrieve (0 for no limit)</param>
    /// <returns>Lista de guilds</returns>
    [HttpGet("all")]
    public async Task<ActionResult<ApiResult<IEnumerable<Guild>>>> GetAllGuilds([FromQuery] int limit = 0)
    {
        try
        {
            System.Collections.Concurrent.ConcurrentBag<Guild> guilds = await _guildRepository.GetAllAsync(limit);
            return Ok(ApiResult<IEnumerable<Guild>>.Ok(guilds.ToList()));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error retrieving all guilds: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<Guild>>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Deletes a guild permanently
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <returns>Success ou erro</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResult<object>>> DeleteGuild(ulong id)
    {
        try
        {
            bool success = await _guildRepository.DeleteAsync(id);

            if (!success)
            {
                return NotFound(ApiResult<object>.Fail($"Guild with ID {id} not found or could not be deleted"));
            }

            return Ok(ApiResult<object>.Ok(null, "Guild deleted successfully"));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error deleting guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Removes guild from cache
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <returns>Success</returns>
    [HttpDelete("{id}/remove-cache")]
    public async Task<ActionResult<ApiResult<object>>> DeleteGuildCache(ulong id)
    {
        try
        {
            await _guildRepository.DeleteCache(id);
            return Ok(ApiResult<object>.Ok(null, "Guild cache cleared successfully"));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error clearing cache for guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Makes guild data persistent in cache (removes expiration)
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <returns>Success ou erro</returns>
    [HttpPost("{id}/persist-cache")]
    public async Task<ActionResult<ApiResult<object>>> PersistGuild(ulong id)
    {
        try
        {
            bool success = await _guildRepository.PersistAsync(id.ToString());

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail("Failed to persist guild data"));
            }

            return Ok(ApiResult<object>.Ok(null, "Guild data persisted successfully"));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error persisting guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Gets the language code configured for this guild
    /// </summary>
    /// <param name="id">Guild ID</param>
    /// <returns>Código de idioma</returns>
    [HttpGet("{id}/language")]
    public async Task<ActionResult<ApiResult<string>>> GetLanguage(ulong id)
    {
        try
        {
            Guild? guild = await _guildRepository.FetchAsync(id);

            if (guild == null)
            {
                return NotFound(ApiResult<string>.Fail($"Guild with ID {id} not found"));
            }

            string language = _guildDataManager.Language(guild);
            return Ok(ApiResult<string>.Ok(language));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error getting language for guild {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<string>.Fail("Internal server error"));
        }
    }

    /// <summary>
    /// Retrieves guilds by specific field value
    /// </summary>
    /// <param name="fieldPath">JSON path to the field (e.g., "preference.lang")</param>
    /// <param name="value">Value to search for</param>
    /// <param name="limit">Maximum number of guilds to return (0 for no limit)</param>
    /// <returns>Lista de guilds</returns>
    [HttpGet("by-field")]
    public async Task<ActionResult<ApiResult<IEnumerable<Guild>>>> GetByField([FromQuery] string fieldPath, [FromQuery] string value, [FromQuery] int limit = 0)
    {
        try
        {
            System.Collections.Concurrent.ConcurrentBag<Guild> guilds = await _guildRepository.GetByFieldAsync(fieldPath, value, limit);
            return Ok(ApiResult<IEnumerable<Guild>>.Ok(guilds.ToList()));
        }
        catch (System.Exception ex)
        {
            await AresLogger.LogAsync("GuildsController", $"Error retrieving guilds by field {fieldPath}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<Guild>>.Fail("Internal server error"));
        }
    }
}