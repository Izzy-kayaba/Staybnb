// Saves important user and status changes immediately for the Admin audit trail.
using Staybnb.Web.Data;
using Staybnb.Web.Models;

namespace Staybnb.Web.Services;

public class ActivityLogger(ApplicationDbContext db, IHttpContextAccessor accessor)
{
    // Saves immediately so the business change and its audit entry share the current transaction.
    public async Task LogAsync(string? userId, string action, string details)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();
    }
}
