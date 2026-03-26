using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Services.DTOs;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        var response = await orderService.CreateOrderAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id)
    {
        return Ok(await orderService.GetOrderAsync(id));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetList([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        return Ok(await orderService.GetOrdersAsync(page, limit));
    }

    [HttpPatch("{id:guid}/address")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] string newAddress)
    {
        await orderService.UpdateAddressAsync(id, newAddress);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await orderService.CancelOrderAsync(id);
        return Ok();
    }
}