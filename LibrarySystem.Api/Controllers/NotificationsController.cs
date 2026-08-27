using System.Security.Claims;
using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = RoleNames.Admin)]
public class NotificationsController :
    ControllerBase
{
    private readonly INotificationService
        _notificationService;

    public NotificationsController(
        INotificationService
            notificationService)
    {
        _notificationService =
            notificationService;
    }

    [HttpGet]
    public async Task<IActionResult>
        GetMyNotifications()
    {
        var userId =
            GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result =
            await _notificationService
                .GetMySummaryAsync(
                    userId);

        return Ok(result);
    }

    [HttpPut("{notificationId:int}/read")]
    public async Task<IActionResult>
        MarkAsRead(
            int notificationId)
    {
        var userId =
            GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var success =
            await _notificationService
                .MarkAsReadAsync(
                    userId,
                    notificationId);

        if (!success)
        {
            return NotFound(
                new
                {
                    message =
                        "Bildirim bulunamadı."
                });
        }

        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult>
        MarkAllAsRead()
    {
        var userId =
            GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        await _notificationService
            .MarkAllAsReadAsync(
                userId);

        return NoContent();
    }

    [HttpDelete("read")]
    public async Task<IActionResult>
        DeleteReadNotifications()
    {
        var userId =
            GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var deletedCount =
            await _notificationService
                .DeleteReadAsync(
                    userId);

        return Ok(
            new
            {
                deletedCount
            });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }
}