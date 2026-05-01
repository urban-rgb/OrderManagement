using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain;
using OrderManagement.Services;
using OrderManagement.Services.DTOs;

namespace OrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        var result = await orderService.CreateOrderAsync(request);

        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id)
    {
        var result = await orderService.GetOrderAsync(id);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetList(
            [FromQuery] Guid? userId,
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = true,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
    {
        var result = await orderService.GetOrdersAsync(page, limit, userId, sortBy, isDescending);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/address")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateOrderAddressRequest request)
    {
        var result = await orderService.UpdateAddressAsync(id, request);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await orderService.CancelOrderAsync(id);
        return HandleResult(result);
    }

    private ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value is bool || result.Value == null ? NoContent() : Ok(result.Value);
        }

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { error = result.ErrorMessage }),
            ErrorType.Conflict => Conflict(new { error = result.ErrorMessage }),
            ErrorType.Validation => BadRequest(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = result.ErrorMessage })
        };
    }
}