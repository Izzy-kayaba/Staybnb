// Protects Host-owned listings, booking decisions, and private check-in documents.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;
using Staybnb.Web.Services;
using Staybnb.Web.ViewModels;

namespace Staybnb.Web.Controllers;

[Authorize(Roles = "Host")]
public class HostDashboardController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    NotificationService notifications,
    ActivityLogger logger,
    FileStorageService files) : Controller
{
    // Loads only the signed-in Host's listings, booking requests, and pending documents.
    public async Task<IActionResult> Index()
    {
        var hostId = userManager.GetUserId(User)!;
        return View(new HostDashboardViewModel
        {
            Properties = await db.HostProperties.AsNoTracking()
                .Include(p => p.Images).Include(p => p.Bookings).Include(p => p.Application).AsSplitQuery()
                .Where(p => p.HostId == hostId).OrderByDescending(p => p.CreatedAt).ToListAsync(),
            PendingBookings = await db.Bookings.AsNoTracking().Include(b => b.Property).Include(b => b.Guest)
                .Where(b => b.Property!.HostId == hostId && b.Status == BookingStatus.Pending)
                .OrderBy(b => b.CheckInDate).ToListAsync(),
            PendingDocuments = await db.GuestDocuments.AsNoTracking()
                .Include(d => d.Booking)!.ThenInclude(b => b!.Property)
                .Include(d => d.UploadedBy)
                .Where(d => d.Booking!.Property!.HostId == hostId && d.Status == DocumentStatus.Pending)
                .OrderBy(d => d.UploadedAt).ToListAsync()
        });
    }

    // Displays editable information only when the listing belongs to the current Host.
    public async Task<IActionResult> EditProperty(int id)
    {
        var hostId = userManager.GetUserId(User)!;
        var property = await db.HostProperties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId);
        if (property is null) return NotFound();
        return View(new EditPropertyViewModel
        {
            Id = property.Id, Title = property.Title, Description = property.Description,
            PricePerNight = property.PricePerNight, CleaningFee = property.CleaningFee,
            ServiceFee = property.ServiceFee, Address = property.Address,
            City = property.City, PropertyType = property.PropertyType
        });
    }

    // Updates the allowed listing fields after repeating the ownership check.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProperty(EditPropertyViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var hostId = userManager.GetUserId(User)!;
        var property = await db.HostProperties.FirstOrDefaultAsync(p => p.Id == model.Id && p.HostId == hostId);
        if (property is null) return NotFound();

        property.Title = model.Title.Trim();
        property.Description = model.Description.Trim();
        property.PricePerNight = model.PricePerNight;
        property.CleaningFee = model.CleaningFee;
        property.ServiceFee = model.ServiceFee;
        property.Address = model.Address.Trim();
        property.City = model.City.Trim();
        property.PropertyType = model.PropertyType;
        await logger.LogAsync(hostId, "PropertyUpdated", $"Property #{property.Id} was updated.");
        TempData["Success"] = "Property details updated.";
        return RedirectToAction(nameof(Index));
    }

    // Allows deactivation at any time but requires approval before activation.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var hostId = userManager.GetUserId(User)!;
        var property = await db.HostProperties.Include(p => p.Application)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId);
        if (property is null) return NotFound();
        if (!property.IsActive && !property.CanActivate)
        {
            TempData["Error"] = "This property cannot be activated until its application is approved.";
            return RedirectToAction(nameof(Index));
        }
        property.IsActive = !property.IsActive;
        await logger.LogAsync(hostId, "PropertyStatusChanged", $"'{property.Title}' active status changed to {property.IsActive}.");
        return RedirectToAction(nameof(Index));
    }

    // Preserves booking history by allowing deletion only when no bookings exist.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var hostId = userManager.GetUserId(User)!;
        var property = await db.HostProperties.Include(p => p.Bookings)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId);
        if (property is null) return NotFound();
        if (property.Bookings.Count > 0)
        {
            TempData["Error"] = "A property with booking history cannot be deleted. Deactivate it instead.";
            return RedirectToAction(nameof(Index));
        }

        db.HostProperties.Remove(property);
        await logger.LogAsync(hostId, "PropertyDeleted", $"Property '{property.Title}' was deleted.");
        TempData["Success"] = "Property deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Lets the owning Host decide a booking that is still pending.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BookingDecision(int id, string decision)
    {
        var hostId = userManager.GetUserId(User)!;
        var booking = await db.Bookings.Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property!.HostId == hostId);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Pending) return BadRequest("Only pending bookings can be changed.");

        booking.Status = decision == "Approve" ? BookingStatus.Approved : BookingStatus.Rejected;
        notifications.Add(booking.GuestId, $"Booking {booking.Status}",
            $"Your booking for {booking.Property!.Title} is now {booking.Status}.", "Booking", "/GuestDashboard");
        await logger.LogAsync(hostId, "BookingStatusChanged", $"Booking #{booking.Id} status changed from Pending to {booking.Status}.");
        TempData["Success"] = $"Booking {booking.Status.ToString().ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    // Displays check-in requirements only for a property owned by the current Host.
    public async Task<IActionResult> ConfigureCheckIn(int id)
    {
        var hostId = userManager.GetUserId(User)!;
        var property = await db.HostProperties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId);
        return property is null ? NotFound() : View(property);
    }

    // Saves the document type and arrival instructions that Guests must follow.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureCheckIn(int id, string checkInRules, DocumentType requiredDocumentType)
    {
        var hostId = userManager.GetUserId(User)!;
        var property = await db.HostProperties.FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId);
        if (property is null) return NotFound();
        if (string.IsNullOrWhiteSpace(checkInRules))
        {
            ModelState.AddModelError(nameof(checkInRules), "Check-in rules are required.");
            return View(property);
        }
        property.CheckInRules = checkInRules.Trim();
        property.RequiredDocumentType = requiredDocumentType;
        await logger.LogAsync(hostId, "CheckInProcessUpdated", $"Check-in requirements for '{property.Title}' were updated.");
        TempData["Success"] = "Check-in process updated.";
        return RedirectToAction(nameof(Index));
    }

    // Streams a private document only to the Host who owns the related property.
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var hostId = userManager.GetUserId(User)!;
        var document = await db.GuestDocuments.AsNoTracking().Include(d => d.Booking)!.ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(d => d.Id == id && d.Booking!.Property!.HostId == hostId);
        if (document is null) return NotFound();
        var path = files.GetPrivateFullPath(document.StoredFilePath);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, "application/octet-stream", document.OriginalFileName);
    }

    // Completes check-in only after the owning Host verifies the pending document.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyDocument(int id, string decision)
    {
        var hostId = userManager.GetUserId(User)!;
        var document = await db.GuestDocuments.Include(d => d.Booking)!.ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(d => d.Id == id && d.Booking!.Property!.HostId == hostId);
        if (document is null) return NotFound();
        if (document.Status != DocumentStatus.Pending) return BadRequest("This document has already been reviewed.");

        var verified = decision == "Verify";
        document.Status = verified ? DocumentStatus.Verified : DocumentStatus.Rejected;
        document.VerifiedAt = DateTime.UtcNow;
        document.VerifiedByHostId = hostId;
        if (verified) document.Booking!.Status = BookingStatus.CheckedIn;

        notifications.Add(document.Booking!.GuestId, verified ? "Check-in complete" : "Document rejected",
            verified ? "Your document was verified and your booking is checked in." : "Please upload a clearer ID or passport.",
            "CheckIn", $"/GuestDashboard/CheckIn/{document.BookingId}");
        await logger.LogAsync(hostId, "BookingStatusChanged",
            verified ? $"Booking #{document.BookingId} status changed from Approved to CheckedIn after document verification."
                     : $"Document #{document.Id} was rejected for booking #{document.BookingId}.");
        return RedirectToAction(nameof(Index));
    }
}
