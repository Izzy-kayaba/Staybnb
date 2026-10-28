// Centralises application review, audit visibility, and SuperAdmin-only promotion.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;
using Staybnb.Web.Services;
using Staybnb.Web.ViewModels;

namespace Staybnb.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    NotificationService notifications,
    ActivityLogger logger) : Controller
{
    // Combines pending applications, recent activity, and SuperAdmin promotion choices.
    public async Task<IActionResult> Index()
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        List<ApplicationUser> promotableGuests = [];
        if (isSuperAdmin)
        {
            // A pending Host applicant must finish that role transition before entering another one.
            var pendingApplicantIds = await db.HostApplications.AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Pending)
                .Select(a => a.ApplicantId)
                .ToListAsync();
            promotableGuests = (await userManager.GetUsersInRoleAsync("Guest"))
                .Where(user => !pendingApplicantIds.Contains(user.Id))
                .OrderBy(user => user.Email)
                .ToList();
        }

        return View(new AdminDashboardViewModel
        {
            PendingApplications = await db.HostApplications.AsNoTracking()
                .Include(a => a.Applicant).Include(a => a.Property)!.ThenInclude(p => p!.Images)
                .Where(a => a.Status == ApplicationStatus.Pending).OrderBy(a => a.SubmittedAt).ToListAsync(),
            RecentActivity = await db.ActivityLogs.AsNoTracking().Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync(),
            Guests = promotableGuests,
            IsSuperAdmin = isSuperAdmin
        });
    }

    // Approves the listing and completes a first-time Guest-to-Host role change.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveApplication(int id)
    {
        var application = await db.HostApplications.Include(a => a.Property).Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (application is null) return NotFound();
        if (application.Status != ApplicationStatus.Pending) return BadRequest("This application has already been reviewed.");

        var applicantIsGuest = await userManager.IsInRoleAsync(application.Applicant!, "Guest");
        var applicantIsHost = await userManager.IsInRoleAsync(application.Applicant!, "Host");
        if (!applicantIsGuest && !applicantIsHost)
            return BadRequest("The applicant must be a Guest or an existing Host.");

        // Identity and application changes share one transaction so partial role swaps cannot persist.
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (applicantIsGuest)
        {
            var addResult = await userManager.AddToRoleAsync(application.Applicant!, "Host");
            if (!addResult.Succeeded) return BadRequest("The Host role could not be added.");
            var removeResult = await userManager.RemoveFromRoleAsync(application.Applicant!, "Guest");
            if (!removeResult.Succeeded) return BadRequest("The Guest role could not be removed.");
            await logger.LogAsync(application.ApplicantId, "RoleChanged", "Role changed from Guest to Host after application approval.");
        }

        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = userManager.GetUserId(User);
        application.Property!.IsActive = true;
        notifications.Add(application.ApplicantId, "Host application approved",
            $"Your application for {application.Property.Title} was approved.", "Application", "/HostDashboard");
        await logger.LogAsync(userManager.GetUserId(User), "HostApplicationApproved", $"Application #{id} was approved.");
        await transaction.CommitAsync();
        TempData["Success"] = "Application approved and roles updated.";
        return RedirectToAction(nameof(Index));
    }

    // Keeps a rejected property inactive and records the reason for the applicant.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectApplication(int id, string? reason)
    {
        var application = await db.HostApplications.Include(a => a.Property).FirstOrDefaultAsync(a => a.Id == id);
        if (application is null) return NotFound();
        if (application.Status != ApplicationStatus.Pending) return BadRequest("This application has already been reviewed.");

        application.Status = ApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = userManager.GetUserId(User);
        application.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "The application did not meet the listing requirements." : reason.Trim();
        application.Property!.IsActive = false;
        notifications.Add(application.ApplicantId, "Host application rejected", application.RejectionReason, "Application", "/HostApplications/Apply");
        await logger.LogAsync(userManager.GetUserId(User), "HostApplicationRejected", $"Application #{id} was rejected.");
        TempData["Success"] = "Application rejected.";
        return RedirectToAction(nameof(Index));
    }

    // Only a SuperAdmin can replace an eligible Guest role with the Admin role.
    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (!await userManager.IsInRoleAsync(user, "Guest")) return BadRequest("Only a Guest can be promoted to Admin.");

        var hasPendingHostApplication = await db.HostApplications.AsNoTracking()
            .AnyAsync(application => application.ApplicantId == user.Id && application.Status == ApplicationStatus.Pending);
        if (hasPendingHostApplication)
            return BadRequest("A Guest with a pending Host application cannot be promoted to Admin.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var addResult = await userManager.AddToRoleAsync(user, "Admin");
        if (!addResult.Succeeded) return BadRequest("The Admin role could not be added.");
        var removeResult = await userManager.RemoveFromRoleAsync(user, "Guest");
        if (!removeResult.Succeeded) return BadRequest("The Guest role could not be removed.");

        notifications.Add(user.Id, "Admin role assigned", "A SuperAdmin promoted your account to Admin.", "Role", "/Admin");
        await logger.LogAsync(user.Id, "RoleChanged", "Role changed from Guest to Admin by a SuperAdmin.");
        await transaction.CommitAsync();
        TempData["Success"] = $"{user.Email} is now an Admin.";
        return RedirectToAction(nameof(Index));
    }
}
