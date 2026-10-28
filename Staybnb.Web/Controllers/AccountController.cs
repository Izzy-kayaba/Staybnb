// Handles account entry points and sends each authenticated role to its own dashboard.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Web.Models;
using Staybnb.Web.Services;
using Staybnb.Web.ViewModels;

namespace Staybnb.Web.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ActivityLogger logger) : Controller
{
    // Displays the form used to create an account that will begin as a Guest.
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    // Creates the account, assigns the Guest role, and signs the new user in.
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Guest");
            await logger.LogAsync(user.Id, "Registration", $"{user.Email} registered and received the Guest role.");
            await signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "GuestDashboard");
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    // Preserves a safe local destination when login was triggered by a protected page.
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    // Validates the password and sends the user to the dashboard for the assigned role.
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                await logger.LogAsync(user.Id, "Login", $"{user.Email} logged in.");
                if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

                if (await userManager.IsInRoleAsync(user, "SuperAdmin") || await userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Admin");
                if (await userManager.IsInRoleAsync(user, "Host"))
                    return RedirectToAction("Index", "HostDashboard");
                return RedirectToAction("Index", "GuestDashboard");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    // Records the logout before clearing the authentication cookie.
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is not null) await logger.LogAsync(user.Id, "Logout", $"{user.Email} logged out.");
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // Shows a friendly explanation when an authenticated role is not allowed through.
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
