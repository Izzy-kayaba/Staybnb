// Serves public discovery while ensuring inactive properties never appear publicly.
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Data;
using Staybnb.Web.Models;

namespace Staybnb.Web.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    // Lists active properties and applies the optional city and property-type filters.
    public async Task<IActionResult> Index(string? city, string? propertyType)
    {
        var query = db.HostProperties.AsNoTracking().Include(p => p.Images).Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(p => p.City.Contains(city));
        if (!string.IsNullOrWhiteSpace(propertyType)) query = query.Where(p => p.PropertyType == propertyType);

        ViewBag.City = city;
        ViewBag.PropertyType = propertyType;
        ViewBag.PropertyTypes = await db.HostProperties.AsNoTracking()
            .Where(p => p.IsActive).Select(p => p.PropertyType).Distinct().OrderBy(t => t).ToListAsync();

        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    // Prevents inactive listings from being opened through a guessed address.
    public async Task<IActionResult> Details(int id)
    {
        var property = await db.HostProperties.AsNoTracking()
            .Include(p => p.Images).Include(p => p.Host)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        return property is null ? NotFound() : View(property);
    }

    // Supplies a request identifier without exposing internal exception details.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
