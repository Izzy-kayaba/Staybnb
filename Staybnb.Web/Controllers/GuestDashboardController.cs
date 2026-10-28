// Restricts booking and check-in operations to the Guest who owns the booking.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;
using Staybnb.Web.Services;
using Staybnb.Web.ViewModels;

namespace Staybnb.Web.Controllers;

[Authorize(Roles = "Guest")]
public class GuestDashboardController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    FileStorageService files,
    NotificationService notifications,
    ActivityLogger logger) : Controller
{
    // Shows only bookings owned by the signed-in Guest.
    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User)!;
        var bookings = await db.Bookings.AsNoTracking()
            .Include(b => b.Property)!.ThenInclude(p => p!.Images)
            .Include(b => b.Documents)
            .AsSplitQuery()
            .Where(b => b.GuestId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return View(bookings);
    }

    // Displays a booking form only for a property that is currently active.
    public async Task<IActionResult> Book(int id)
    {
        var property = await db.HostProperties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (property is null) return NotFound();
        return View(ToBookingViewModel(property));
    }

    // Rechecks dates and calculates the trusted total from prices stored in the database.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookPropertyViewModel model)
    {
        var property = await db.HostProperties.FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);
        if (property is null) return NotFound();

        if (model.CheckInDate.Date < DateTime.Today) ModelState.AddModelError(nameof(model.CheckInDate), "Check-in cannot be in the past.");
        if (model.CheckOutDate.Date <= model.CheckInDate.Date) ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out must be after check-in.");

        if (!ModelState.IsValid)
        {
            var display = ToBookingViewModel(property);
            display.CheckInDate = model.CheckInDate;
            display.CheckOutDate = model.CheckOutDate;
            display.NumberOfTravellers = model.NumberOfTravellers;
            return View(display);
        }

        var total = BookingPriceCalculator.Calculate(
            property.PricePerNight, model.CheckInDate, model.CheckOutDate, property.CleaningFee, property.ServiceFee);
        var userId = userManager.GetUserId(User)!;
        var booking = new Booking
        {
            PropertyId = property.Id,
            GuestId = userId,
            CheckInDate = model.CheckInDate.Date,
            CheckOutDate = model.CheckOutDate.Date,
            NumberOfTravellers = model.NumberOfTravellers,
            TotalPrice = total,
            Status = BookingStatus.Pending
        };

        db.Bookings.Add(booking);
        notifications.Add(property.HostId, "New booking request", $"A guest requested {property.Title}.", "Booking", "/HostDashboard");
        await logger.LogAsync(userId, "BookingCreated", $"Booking for '{property.Title}' was created with total R{total:N2} and Pending status.");
        TempData["Success"] = $"Booking request sent. Total price: R{total:N2}.";
        return RedirectToAction(nameof(Index));
    }

    // Opens check-in only for the Guest's approved or completed booking.
    public async Task<IActionResult> CheckIn(int id)
    {
        var userId = userManager.GetUserId(User)!;
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.Property).Include(b => b.Documents)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);
        if (booking is null) return NotFound();
        if (booking.Status is not BookingStatus.Approved and not BookingStatus.CheckedIn) return BadRequest("The booking must be approved before check-in.");
        return View(booking);
    }

    // Stores one allowed document privately for the Host to review.
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(CheckInUploadViewModel model)
    {
        var userId = userManager.GetUserId(User)!;
        var booking = await db.Bookings.Include(b => b.Property).Include(b => b.Documents)
            .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.GuestId == userId);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Approved) return BadRequest("Only approved bookings can continue to check-in.");
        if (booking.Documents.Any(d => d.Status != DocumentStatus.Rejected))
        {
            TempData["Error"] = "A document is already awaiting review or has been verified.";
            return RedirectToAction(nameof(CheckIn), new { id = booking.Id });
        }
        if (model.Document is null || model.Document.Length == 0)
        {
            TempData["Error"] = "Choose an ID or passport file to upload.";
            return RedirectToAction(nameof(CheckIn), new { id = booking.Id });
        }

        string storedPath;
        try
        {
            storedPath = await files.SavePrivateAsync(model.Document, "documents", [".jpg", ".jpeg", ".png", ".pdf"]);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(CheckIn), new { id = booking.Id });
        }

        db.GuestDocuments.Add(new GuestDocument
        {
            BookingId = booking.Id,
            UploadedById = userId,
            DocumentType = booking.Property!.RequiredDocumentType,
            OriginalFileName = Path.GetFileName(model.Document.FileName),
            StoredFilePath = storedPath
        });
        notifications.Add(booking.Property.HostId, "Check-in document uploaded", $"A guest uploaded a document for {booking.Property.Title}.", "CheckIn", "/HostDashboard");
        await logger.LogAsync(userId, "GuestDocumentUploaded", $"A check-in document was uploaded for booking #{booking.Id}.");
        TempData["Success"] = "Document uploaded. The host must verify it to complete check-in.";
        return RedirectToAction(nameof(CheckIn), new { id = booking.Id });
    }

    // Copies display values from the trusted property without accepting them from the browser.
    private static BookPropertyViewModel ToBookingViewModel(HostProperty property) => new()
    {
        PropertyId = property.Id,
        PropertyTitle = property.Title,
        PricePerNight = property.PricePerNight,
        CleaningFee = property.CleaningFee,
        ServiceFee = property.ServiceFee
    };
}
