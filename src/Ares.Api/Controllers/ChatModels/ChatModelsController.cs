using Ares.Core.Manager;
using Ares.Core.Models.Data;
using Ares.Core.Objects;
using Ares.Core.Repository;
using Ares.Core.Util;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Ares.Api.Controllers.ChatModels;

[ApiController]
[Route("api/chat-models")]
public class ChatModelsController : ControllerBase
{
    private readonly ChatModelRepository _chatModelRepository;
    private readonly ChatModelDataManager _chatModelDataManager;

    /// <summary>
    /// Initializes a new instance of the ChatModelsController class with required dependencies.
    /// </summary>
    /// <param name="chatModelRepository">Repository for chat model operations</param>
    /// <param name="chatModelDataManager">Manager for chat model data operations</param>
    public ChatModelsController(ChatModelRepository chatModelRepository, ChatModelDataManager chatModelDataManager)
    {
        _chatModelRepository = chatModelRepository;
        _chatModelDataManager = chatModelDataManager;
    }

    /// <summary>
    /// Creates or retrieves a chat model by ID
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>ChatModel object or error</returns>
    [HttpPost("{id}/create-or-get")]
    public async Task<ActionResult<ChatModel>> CreateOrGetModel(string id)
    {
        try
        {
            ChatModel? model = await _chatModelRepository.SaveAsync(id);

            if (model == null)
            {
                return StatusCode(500, new { message = "Failed to create or retrieve chat model" });
            }

            return Ok(model);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error creating/getting model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates or updates a chat model
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="model">Model data to save</param>
    /// <returns>Updated model or error</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ChatModel>> SaveModel(string id, [FromBody] ChatModel model)
    {
        try
        {
            if (model.Id != id)
            {
                return BadRequest(new { message = "Model ID in URL doesn't match model object ID" });
            }

            ChatModel? savedModel = await _chatModelRepository.SaveAsync(id, model);

            if (savedModel == null)
            {
                return StatusCode(500, new { message = "Failed to save chat model" });
            }

            return Ok(savedModel);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error saving model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves a chat model by ID
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="saveInRedis">Whether to save in Redis cache if fetched from database</param>
    /// <returns>ChatModel object or not found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChatModel>> GetModel(string id, [FromQuery] bool saveInRedis = false)
    {
        try
        {
            ChatModel? model = await _chatModelRepository.FetchAsync(id, saveInRedis);

            if (model == null)
            {
                return NotFound(new { message = $"Chat model with ID {id} not found" });
            }

            return Ok(model);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error fetching model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves a chat model by nearest matching ID
    /// </summary>
    /// <param name="id">Base model ID</param>
    /// <param name="saveInRedis">Whether to save in Redis cache if fetched from database</param>
    /// <returns>ChatModel object or not found</returns>
    [HttpGet("{id}/nearest")]
    public async Task<ActionResult<ChatModel>> GetNearestModel(string id, [FromQuery] bool saveInRedis = false)
    {
        try
        {
            ChatModel? model = await _chatModelRepository.FetchByNearestModelAsync(id, saveInRedis);

            if (model == null)
            {
                return NotFound(new { message = $"No chat model found with ID similar to {id}" });
            }

            return Ok(model);
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error fetching nearest model for {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates a specific field of a chat model
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="model">Updated model data</param>
    /// <param name="field">Field name to update</param>
    /// <returns>Success or error response</returns>
    [HttpPut("{id}/update-field")]
    public async Task<ActionResult> UpdateModelField(string id, [FromBody] ChatModel model, [FromQuery] string field)
    {
        try
        {
            if (model.Id != id)
            {
                return BadRequest(new { message = "Model ID in URL doesn't match model object ID" });
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return BadRequest(new { message = "Field name is required" });
            }

            bool success = await _chatModelRepository.UpdateAsync(model, field);

            if (!success)
            {
                return StatusCode(500, new { message = $"Failed to update field '{field}'" });
            }

            return Ok(new { message = $"Field '{field}' updated successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error updating field '{field}' for model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates multiple fields of a chat model
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="model">Updated model data</param>
    /// <param name="fields">Comma-separated list of fields to update</param>
    /// <returns>Success or error response</returns>
    [HttpPut("{id}/update-fields")]
    public async Task<ActionResult> UpdateModelFields(string id, [FromBody] ChatModel model, [FromQuery] string fields)
    {
        try
        {
            if (model.Id != id)
            {
                return BadRequest(new { message = "Model ID in URL doesn't match model object ID" });
            }

            if (string.IsNullOrWhiteSpace(fields))
            {
                return BadRequest(new { message = "Fields list is required" });
            }

            string[] fieldArray = fields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            if (fieldArray.Length == 0)
            {
                return BadRequest(new { message = "At least one valid field must be specified" });
            }

            bool success = await _chatModelDataManager.SaveAsync(model, fieldArray);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to update fields" });
            }

            return Ok(new { message = "Fields updated successfully", updatedFields = fieldArray });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error updating fields for model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Retrieves all chat models with optional limit
    /// </summary>
    /// <param name="limit">Maximum number of models to retrieve (0 for no limit)</param>
    /// <returns>List of chat models</returns>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ChatModel>>> GetAllModels([FromQuery] int limit = 0)
    {
        try
        {
            ConcurrentBag<ChatModel> models = await _chatModelRepository.GetAllAsync(limit);
            return Ok(models.ToList());
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error retrieving all models: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a chat model permanently
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>Success or error response</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteModel(string id)
    {
        try
        {
            bool success = await _chatModelRepository.DeleteAsync(id);

            if (!success)
            {
                return NotFound(new { message = $"Chat model with ID {id} not found or could not be deleted" });
            }

            return Ok(new { message = "Chat model deleted successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error deleting model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Removes chat model from cache
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}/remove-cache")]
    public async Task<ActionResult> DeleteModelCache(string id)
    {
        try
        {
            await _chatModelRepository.DeleteCache(id);
            return Ok(new { message = "Chat model cache cleared successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error clearing cache for model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Makes chat model data persistent in cache (removes expiration)
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>Success or error response</returns>
    [HttpPost("{id}/persist-cache")]
    public async Task<ActionResult> PersistModel(string id)
    {
        try
        {
            bool success = await _chatModelRepository.PersistAsync(id);

            if (!success)
            {
                return StatusCode(500, new { message = "Failed to persist chat model data" });
            }

            return Ok(new { message = "Chat model data persisted successfully" });
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error persisting model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}