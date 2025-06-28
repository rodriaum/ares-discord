using Ares.Common.DTOs;
using Ares.Common.Manager;
using Ares.Common.Models.Data;
using Ares.Common.Objects;
using Ares.Common.Repository;
using Ares.Common.Util;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Ares.Api.Controllers.ChatModels;

[ApiController]
[Route("api/chat-models")]
public class ChatModelsController : ControllerBase
{
    private readonly ChatModelRepository _chatModelRepository;
    private readonly ChatModelDataManager _chatModelDataManager;

    public ChatModelsController(ChatModelRepository chatModelRepository, ChatModelDataManager chatModelDataManager)
    {
        _chatModelRepository = chatModelRepository;
        _chatModelDataManager = chatModelDataManager;
    }

    [HttpPost("{id}/create-or-get")]
    public async Task<ActionResult<ApiResult<ChatModel>>> CreateOrGetModel(string id)
    {
        try
        {
            ChatModel? model = await _chatModelRepository.SaveAsync(id);

            if (model == null)
            {
                return StatusCode(500, ApiResult<ChatModel>.Fail("Failed to create or retrieve chat model"));
            }

            return Ok(ApiResult<ChatModel>.Ok(model));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error creating/getting model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<ChatModel>.Fail("Internal server error"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResult<ChatModel>>> SaveModel(string id, [FromBody] ChatModel model)
    {
        try
        {
            if (model.Id != id)
            {
                return BadRequest(ApiResult<ChatModel>.Fail("Model ID in URL doesn't match model object ID"));
            }

            ChatModel? savedModel = await _chatModelRepository.SaveAsync(id, model);

            if (savedModel == null)
            {
                return StatusCode(500, ApiResult<ChatModel>.Fail("Failed to save chat model"));
            }

            return Ok(ApiResult<ChatModel>.Ok(savedModel));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error saving model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<ChatModel>.Fail("Internal server error"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResult<ChatModel>>> GetModel(string id, [FromQuery] bool saveInRedis = false)
    {
        try
        {
            ChatModel? model = await _chatModelRepository.FetchAsync(id, saveInRedis);

            if (model == null)
            {
                return NotFound(ApiResult<ChatModel>.Fail($"Chat model with ID {id} not found"));
            }

            return Ok(ApiResult<ChatModel>.Ok(model));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error fetching model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<ChatModel>.Fail("Internal server error"));
        }
    }

    [HttpGet("{id}/nearest")]
    public async Task<ActionResult<ApiResult<ChatModel>>> GetNearestModel(string id, [FromQuery] bool saveInRedis = false)
    {
        try
        {
            ChatModel? model = await _chatModelRepository.FetchByNearestModelAsync(id, saveInRedis);

            if (model == null)
            {
                return NotFound(ApiResult<ChatModel>.Fail($"No chat model found with ID similar to {id}"));
            }

            return Ok(ApiResult<ChatModel>.Ok(model));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error fetching nearest model for {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<ChatModel>.Fail("Internal server error"));
        }
    }

    [HttpPut("{id}/update-field")]
    public async Task<ActionResult<ApiResult<object>>> UpdateModelField(string id, [FromBody] ChatModel model, [FromQuery] string field)
    {
        try
        {
            if (model.Id != id)
            {
                return BadRequest(ApiResult<object>.Fail("Model ID in URL doesn't match model object ID"));
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return BadRequest(ApiResult<object>.Fail("Field name is required"));
            }

            bool success = await _chatModelRepository.UpdateAsync(model, field);

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Failed to update field '{field}'"));
            }

            return Ok(ApiResult<object>.Ok(null, $"Field '{field}' updated successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error updating field '{field}' for model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpPut("{id}/update-fields")]
    public async Task<ActionResult<ApiResult<object>>> UpdateModelFields(string id, [FromBody] ChatModel model, [FromQuery] string fields)
    {
        try
        {
            if (model.Id != id)
            {
                return BadRequest(ApiResult<object>.Fail("Model ID in URL doesn't match model object ID"));
            }

            if (string.IsNullOrWhiteSpace(fields))
            {
                return BadRequest(ApiResult<object>.Fail("Fields list is required"));
            }

            string[] fieldArray = fields.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (fieldArray.Length == 0)
            {
                return BadRequest(ApiResult<object>.Fail("At least one valid field must be specified"));
            }

            bool success = await _chatModelDataManager.SaveAsync(model, fieldArray);

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail("Failed to update fields"));
            }

            return Ok(ApiResult<object>.Ok(fieldArray, "Fields updated successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error updating fields for model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResult<IEnumerable<ChatModel>>>> GetAllModels([FromQuery] int limit = 0)
    {
        try
        {
            ConcurrentBag<ChatModel> models = await _chatModelRepository.GetAllAsync(limit);
            List<ChatModel> modelList = models.ToList();
            return Ok(ApiResult<IEnumerable<ChatModel>>.Ok(modelList));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error retrieving all models: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<IEnumerable<ChatModel>>.Fail("Internal server error"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResult<object>>> DeleteModel(string id)
    {
        try
        {
            bool success = await _chatModelRepository.DeleteAsync(id);

            if (!success)
            {
                return NotFound(ApiResult<object>.Fail($"Chat model with ID {id} not found or could not be deleted"));
            }

            return Ok(ApiResult<object>.Ok(null, "Chat model deleted successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error deleting model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpDelete("{id}/remove-cache")]
    public async Task<ActionResult<ApiResult<object>>> DeleteModelCache(string id)
    {
        try
        {
            await _chatModelRepository.DeleteCache(id);
            return Ok(ApiResult<object>.Ok(null, "Chat model cache cleared successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error clearing cache for model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }

    [HttpPost("{id}/persist-cache")]
    public async Task<ActionResult<ApiResult<object>>> PersistModel(string id)
    {
        try
        {
            bool success = await _chatModelRepository.PersistAsync(id);

            if (!success)
            {
                return StatusCode(500, ApiResult<object>.Fail("Failed to persist chat model data"));
            }

            return Ok(ApiResult<object>.Ok(null, "Chat model data persisted successfully"));
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("ChatModelsController", $"Error persisting model {id}: {ex.Message}", severity: Severity.Error);
            return StatusCode(500, ApiResult<object>.Fail("Internal server error"));
        }
    }
}
