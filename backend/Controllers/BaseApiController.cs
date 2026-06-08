using backend.Domain;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value is bool || result.Value == null ? NoContent() : Ok(result.Value);

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { error = result.ErrorMessage }),
            ErrorType.Conflict => Conflict(new { error = result.ErrorMessage }),
            ErrorType.Validation => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = result.ErrorMessage })
        };
    }
}
