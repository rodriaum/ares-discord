using Ares.Common.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Ares.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<ApiResult<string>> GetStatus()
    {
        return Ok(ApiResult<string>.Ok(null, "API is online"));
    }
}