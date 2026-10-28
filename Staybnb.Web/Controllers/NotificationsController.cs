// Keeps every notification query scoped to the signed-in account.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;

namespace Staybnb.Web.Controllers;

[Authorize]
public class NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    // Lists notifications belonging only to the signed-in user.
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User)!;
        return View(await db.Notifications.AsNoTracking().Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt).ToListAsync());
    }

    // Marks one owned notification as read before following its safe local link.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null) return NotFound();
        notification.IsRead = true;
        await db.SaveChangesAsync();
        return !string.IsNullOrWhiteSpace(notification.Link) && Url.IsLocalUrl(notification.Link)
            ? LocalRedirect(notification.Link)
            : RedirectToAction(nameof(Index));
    }

    // Updates all unread notifications for the current account in one save.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = userManager.GetUserId(User)!;
        var unread = await db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var notification in unread) notification.IsRead = true;
        await db.SaveChangesAsync();
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }
}
