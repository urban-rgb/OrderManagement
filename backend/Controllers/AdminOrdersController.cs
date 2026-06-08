using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Domain;
using backend.Services;
using backend.Services.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(IOrderService orderService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetList(
        [FromQuery] string? sortBy,
        [FromQuery] bool isDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        OrderStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var s))
            parsedStatus = s;

        var result = await orderService.GetOrdersAsync(page, limit, userId, sortBy, isDescending, parsedStatus, dateFrom, dateTo);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id)
    {
        var result = await orderService.GetOrderAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ForceUpdateStatus(Guid id, [FromBody] ForceUpdateStatusRequest request)
    {
        var result = await orderService.ForceUpdateStatusAsync(id, request.NewStatus);
        return HandleResult(result);
    }
}
