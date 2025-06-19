using Ares.Core.Models.Data;
using Ares.Core.Objects;
using Ares.Core.Repository;
using Ares.Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Ares.Api.Controllers.Users;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public UsersController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Creates or retrieves a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User object or error</returns>
    [HttpPost("{id}/create-or-get")]
    public async Task<ActionResult<User>> CreateOrGetUser(ulong id)
    {
        try
        {
            var user = await _userRepository.SaveAsync(id);

            if (user == null)
            {
                return StatusCode(500, new { message = "Failed to create or retrieve user" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error creating/getting user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="useCache">Whether to save in Redis cache if fetched from database</param>
    /// <returns>User object or not found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(ulong id, [FromQuery] bool useCache = true)
    {
        try
        {
            var user = await _userRepository.FetchAsync(id, saveInRedis: useCache);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error fetching user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="user">Updated user object</param>
    /// <param name="field">Field name being updated (for logging)</param>
    /// <returns>Success or error response</returns>
    [HttpPut("{id}/update")]
    public async Task<ActionResult> UpdateUser(ulong id, [FromBody] User user, [FromQuery] string field = "data")
    {
        try
        {
            if (user.Id != id)
            {
                return BadRequest(new { message = "User ID in URL doesn't match user object ID" });
            }

            var success = await _userRepository.UpdateAsync(user, field);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to update user" });
            }

            return Ok(new { message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error updating user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves all users with optional limit
    /// </summary>
    /// <param name="limit">Maximum number of users to retrieve (0 for no limit)</param>
    /// <returns>List of users</returns>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers([FromQuery] int limit = 0)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(limit);
            return Ok(users.ToList());
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error retrieving all users: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a user permanently
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error response</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(ulong id)
    {
        try
        {
            var success = await _userRepository.DeleteAsync(id);

            if (!success)
            {
                return NotFound(new { message = $"User with ID {id} not found or could not be deleted" });
            }

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error deleting user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Removes user from cache
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}/cache/remove")]
    public async Task<ActionResult> DeleteUserCache(ulong id)
    {
        try
        {
            await _userRepository.DeleteCache(id);
            return Ok(new { message = "User cache cleared successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error clearing cache for user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Makes user data persistent in cache (removes expiration)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error response</returns>
    [HttpPost("{id}/cache/persist")]
    public async Task<ActionResult> PersistUser(ulong id)
    {
        try
        {
            var success = await _userRepository.PersistAsync(id.ToString());

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to persist user data" });
            }

            return Ok(new { message = "User data persisted successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error persisting user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}