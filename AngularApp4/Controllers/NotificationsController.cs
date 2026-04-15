using System.Security.Claims;
using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppNotificationDto>>>> GetMine([FromQuery] int limit = 12, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var take = Math.Clamp(limit, 1, 50);

        var items = await _db.AppNotifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId && !x.IsDismissed && x.ScheduledForUtc <= now)
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new AppNotificationDto
            {
                AppNotificationId = x.AppNotificationId,
                Category = x.Category,
                NotificationType = x.NotificationType,
                Title = x.Title,
                Message = x.Message,
                ActionUrl = x.ActionUrl,
                IsRead = x.IsRead,
                ScheduledForUtc = x.ScheduledForUtc,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<AppNotificationDto>>.Ok(items));
    }

    [HttpPost("{notificationId:long}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(long notificationId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var item = await _db.AppNotifications.FirstOrDefaultAsync(x => x.AppNotificationId == notificationId && x.RecipientUserId == userId, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Notification not found"));
        }

        item.IsRead = true;
        item.ReadAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Notification marked as read"));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var items = await _db.AppNotifications
            .Where(x => x.RecipientUserId == userId && !x.IsDismissed && !x.IsRead && x.ScheduledForUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Notifications marked as read"));
    }

    private long GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.Parse(raw!);
    }
}
