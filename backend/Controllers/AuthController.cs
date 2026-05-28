using Microsoft.AspNetCore.Mvc;
using backend.Domain;
using backend.Services;
using backend.Services.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return HandleResult(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return HandleResult(result);
    }

    private ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorType switch
        {
            ErrorType.Conflict => Conflict(new { error = result.ErrorMessage }),
            ErrorType.Validation => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = result.ErrorMessage })
        };
    }
}
