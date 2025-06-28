using Ares.Common.DTOs;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Common.Repository;
using Ares.Common.Util;
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

    [HttpPost("{id}/create-or-get")]
    public async Task<ActionResult<ApiResult<User>>> CreateOrGetUser(ulong id)
    {
        try
        {
            var user = await _userRepository.SaveAsync(id);

            if (user == null)
                return StatusCode(500, ApiResult<User>.Fail("Failed to create or retrieve user"));

            return Ok(ApiResult<User>.Ok(user));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error creating/getting user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<User>.Fail("Internal server error"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResult<User>>> GetUser(ulong id, [FromQuery] bool useCache = true)
    {
        try
        {
            var user = await _userRepository.FetchAsync(id, saveInRedis: useCache);

            if (user == null)
                return NotFound(ApiResult<User>.Fail($"User with ID {id} not found"));

            return Ok(ApiResult<User>.Ok(user));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error fetching user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<User>.Fail("Internal server error"));
        }
    }

    [HttpPut("{id}/update")]
    public async Task<ActionResult<ApiResult<object>>> UpdateUser(ulong id, [FromBody] User user, [FromQuery] string field = "data")
    {
        try
        {
            if (user.Id != id)
                return BadRequest(ApiResult<object>.Fail("User ID in URL doesn't match user object ID"));

            var success = await _userRepository.UpdateAsync(user, field);

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to update user"));

            return Ok(ApiResult<object>.Ok(null, "User updated successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error updating user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResult<IEnumerable<User>>>> GetAllUsers([FromQuery] int limit = 0)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(limit);
            return Ok(ApiResult<IEnumerable<User>>.Ok(users.ToList()));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error retrieving all users: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<User>>.Fail("Internal server error"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResult<object>>> DeleteUser(ulong id)
    {
        try
        {
            var success = await _userRepository.DeleteAsync(id);

            if (!success)
                return NotFound(ApiResult<object>.Fail($"User with ID {id} not found or could not be deleted"));

            return Ok(ApiResult<object>.Ok(null, "User deleted successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error deleting user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpDelete("{id}/cache/remove")]
    public async Task<ActionResult<ApiResult<object>>> DeleteUserCache(ulong id)
    {
        try
        {
            await _userRepository.DeleteCache(id);
            return Ok(ApiResult<object>.Ok(null, "User cache cleared successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error clearing cache for user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpPost("{id}/cache/persist")]
    public async Task<ActionResult<ApiResult<object>>> PersistUser(ulong id)
    {
        try
        {
            var success = await _userRepository.PersistAsync(id.ToString());

            if (!success)
                return StatusCode(500, ApiResult<object>.Fail("Failed to persist user data"));

            return Ok(ApiResult<object>.Ok(null, "User data persisted successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("UsersController", $"Error persisting user {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }
}