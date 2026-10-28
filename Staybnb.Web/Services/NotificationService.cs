// Adds notification rows to the same request transaction as the event that created them.
using Staybnb.Web.Data;
using Staybnb.Web.Models;

namespace Staybnb.Web.Services;

public class NotificationService(ApplicationDbContext db)
{
    // Adds the notification to the current database unit of work without saving independently.
    public void Add(string userId, string title, string body, string type, string? link = null)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            Link = link
        });
    }
}
