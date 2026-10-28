// Stores private user-to-user messages and updates received-message read state.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;
using Staybnb.Web.Services;
using Staybnb.Web.ViewModels;

namespace Staybnb.Web.Controllers;

[Authorize]
public class MessagesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    NotificationService notifications) : Controller
{
    // Shows conversations where the signed-in user is either sender or recipient.
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User)!;
        ViewBag.CurrentUserId = userId;
        return View(await db.Messages.AsNoTracking().Include(m => m.Sender).Include(m => m.Recipient)
            .Where(m => m.SenderId == userId || m.RecipientId == userId)
            .OrderByDescending(m => m.SentAt).ToListAsync());
    }

    // Marks an incoming message as read only after confirming participation in the conversation.
    public async Task<IActionResult> Read(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var message = await db.Messages.Include(m => m.Sender).Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == id && (m.SenderId == userId || m.RecipientId == userId));
        if (message is null) return NotFound();
        if (message.RecipientId == userId && !message.IsRead)
        {
            message.IsRead = true;
            await db.SaveChangesAsync();
        }
        ViewBag.CurrentUserId = userId;
        return View(message);
    }

    // Optionally prepares a new message with the recipient already selected.
    public IActionResult Compose(string? recipient = null) => View(new ComposeMessageViewModel { RecipientEmail = recipient ?? string.Empty });

    // Creates a reply addressed to the other participant in an authorised conversation.
    public async Task<IActionResult> Reply(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var message = await db.Messages.AsNoTracking().Include(m => m.Sender).Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == id && (m.SenderId == userId || m.RecipientId == userId));
        if (message is null) return NotFound();
        var otherUser = message.SenderId == userId ? message.Recipient : message.Sender;
        return View("Compose", new ComposeMessageViewModel
        {
            RecipientEmail = otherUser!.Email!,
            Subject = message.Subject.StartsWith("Re: ") ? message.Subject : $"Re: {message.Subject}",
            ReplyToMessageId = message.Id
        });
    }

    // Saves the message and creates a notification for the recipient.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Compose(ComposeMessageViewModel model)
    {
        var senderId = userManager.GetUserId(User)!;
        var recipient = await userManager.FindByEmailAsync(model.RecipientEmail.Trim());
        if (recipient is null) ModelState.AddModelError(nameof(model.RecipientEmail), "No Staybnb user has this email address.");
        if (recipient?.Id == senderId) ModelState.AddModelError(nameof(model.RecipientEmail), "You cannot send a message to yourself.");
        if (!ModelState.IsValid) return View(model);

        db.Messages.Add(new Message
        {
            SenderId = senderId,
            RecipientId = recipient!.Id,
            Subject = model.Subject.Trim(),
            Body = model.Body.Trim(),
            ReplyToMessageId = model.ReplyToMessageId
        });
        notifications.Add(recipient.Id, "New message", $"New message: {model.Subject.Trim()}", "Message", "/Messages");
        await db.SaveChangesAsync();
        TempData["Success"] = "Message sent.";
        return RedirectToAction(nameof(Index));
    }
}
