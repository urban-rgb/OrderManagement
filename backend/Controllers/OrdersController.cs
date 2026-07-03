using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Domain;
using backend.Services;
using backend.Services.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User")]
public class OrdersController(IOrderService orderService) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        var result = await orderService.CreateOrderAsync(request, GetCurrentUserId());

        if (!result.IsSuccess)
            return HandleResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id)
    {
        var result = await orderService.GetOrderAsync(id);

        if (!result.IsSuccess)
            return HandleResult(result);

        if (result.Value!.UserId != GetCurrentUserId())
            return Forbid();

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetList(
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = true,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
    {
        var result = await orderService.GetOrdersAsync(page, limit, GetCurrentUserId(), sortBy, isDescending);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/address")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateOrderAddressRequest request)
    {
        var ownerCheck = await orderService.GetOrderAsync(id);

        if (!ownerCheck.IsSuccess)
            return HandleResult(ownerCheck);

        if (ownerCheck.Value!.UserId != GetCurrentUserId())
            return Forbid();

        var result = await orderService.UpdateAddressAsync(id, request);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ownerCheck = await orderService.GetOrderAsync(id);

        if (!ownerCheck.IsSuccess)
            return HandleResult(ownerCheck);

        if (ownerCheck.Value!.UserId != GetCurrentUserId())
            return Forbid();

        var result = await orderService.CancelOrderAsync(id);
        return HandleResult(result);
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue("sub")!);
}
