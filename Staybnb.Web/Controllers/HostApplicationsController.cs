// Collects a property and its required image before Admin review can begin.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;
using Staybnb.Web.Services;
using Staybnb.Web.ViewModels;

namespace Staybnb.Web.Controllers;

[Authorize(Roles = "Guest,Host")]
public class HostApplicationsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    FileStorageService files,
    ActivityLogger logger) : Controller
{
    // Shows whether the current Guest or Host already has an application awaiting review.
    public async Task<IActionResult> Apply()
    {
        var userId = userManager.GetUserId(User)!;
        ViewBag.HasPendingApplication = await db.HostApplications
            .AnyAsync(a => a.ApplicantId == userId && a.Status == ApplicationStatus.Pending);
        return View(new HostApplicationViewModel());
    }

    // Creates an inactive property and its linked application after validating the required image.
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Apply(HostApplicationViewModel model)
    {
        var userId = userManager.GetUserId(User)!;
        if (await db.HostApplications.AnyAsync(a => a.ApplicantId == userId && a.Status == ApplicationStatus.Pending))
            ModelState.AddModelError(string.Empty, "You already have an application awaiting review.");
        if (model.PropertyImage is null || model.PropertyImage.Length == 0)
            ModelState.AddModelError(nameof(model.PropertyImage), "At least one property image is required.");
        if (!ModelState.IsValid) return View(model);

        string imagePath;
        try
        {
            imagePath = await files.SaveAsync(model.PropertyImage!, "properties", [".jpg", ".jpeg", ".png", ".webp"]);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.PropertyImage), ex.Message);
            return View(model);
        }

        // New submissions remain inactive until an Admin approves the linked application.
        var property = new HostProperty
        {
            Title = model.Title.Trim(), Description = model.Description.Trim(), PricePerNight = model.PricePerNight,
            CleaningFee = model.CleaningFee, ServiceFee = model.ServiceFee, Address = model.Address.Trim(),
            City = model.City.Trim(), PropertyType = model.PropertyType, HostId = userId, IsActive = false,
            Images = [new PropertyImage { ImageUrl = imagePath }]
        };

        db.HostApplications.Add(new HostApplication { ApplicantId = userId, Property = property });
        await logger.LogAsync(userId, "HostApplicationSubmitted", $"Application for '{property.Title}' was submitted with an image.");
        TempData["Success"] = "Your application was submitted for admin review.";
        return RedirectToAction(nameof(Apply));
    }
}
