using System.Security.Claims;
using LibrarySystem.Application.Interfaces.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/telegram-notifications")]
[Authorize(Policy = "BorrowerOnly")]
public class TelegramNotificationsController :
    ControllerBase
{
    private readonly
        ITelegramConnectionService
            _telegramConnectionService;

    public TelegramNotificationsController(
        ITelegramConnectionService
            telegramConnectionService)
    {
        _telegramConnectionService =
            telegramConnectionService;
    }

    [HttpGet("status")]
    public async Task<IActionResult>
        GetStatus()
    {
        var userId =
            GetCurrentUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var result =
            await _telegramConnectionService
                .GetStatusAsync(
                    userId);

        return Ok(result);
    }

    [HttpPost("connection-link")]
    public async Task<IActionResult>
        CreateConnectionLink()
    {
        var userId =
            GetCurrentUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var result =
            await _telegramConnectionService
                .CreateConnectionLinkAsync(
                    userId);

        return Ok(result);
    }

    [HttpDelete("connection")]
    public async Task<IActionResult>
        Disconnect()
    {
        var userId =
            GetCurrentUserId();

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        await _telegramConnectionService
            .DisconnectAsync(
                userId);

        return NoContent();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }
}
