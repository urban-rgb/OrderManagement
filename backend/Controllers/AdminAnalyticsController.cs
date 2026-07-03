using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Services.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController(IOrderService orderService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<AnalyticsResponse>> Get()
    {
        var result = await orderService.GetAnalyticsAsync();
        return HandleResult(result);
    }
}