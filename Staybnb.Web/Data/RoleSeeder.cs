// Creates repeatable local roles, demonstration accounts, and marking data on first startup.
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Models;

namespace Staybnb.Web.Data;

public static class RoleSeeder
{
    // These are the only roles used by the role-specific dashboards.
    private static readonly string[] Roles = ["Guest", "Host", "Admin", "SuperAdmin"];

    // Creates repeatable demonstration data only when the database is empty.
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Fixed local accounts let a marker demonstrate every role without registration setup.
        var superAdmin = await EnsureUserAsync(userManager, "superadmin@staybnb.local", "Admin123!", "Super", "Admin", "SuperAdmin");
        var admin = await EnsureUserAsync(userManager, "admin@staybnb.local", "Admin123!", "Amina", "Admin", "Admin");
        var host = await EnsureUserAsync(userManager, "host@staybnb.local", "Host123!", "Hannah", "Host", "Host");
        var guest = await EnsureUserAsync(userManager, "guest@staybnb.local", "Guest123!", "Grace", "Guest", "Guest");
        var applicant = await EnsureUserAsync(userManager, "applicant@staybnb.local", "Guest123!", "Alex", "Applicant", "Guest");

        // Existing user data is preserved. Only old demonstration placeholders are refreshed.
        if (await db.HostProperties.AnyAsync())
        {
            await RefreshDemonstrationListingsAsync(db, host.Id);
            return;
        }

        // Two approved listings and one pending submission demonstrate both visibility states.
        var loft = new HostProperty
        {
            Title = "Table Mountain Loft",
            Description = "A bright city loft with mountain views, fast Wi-Fi and a simple self check-in process.",
            PricePerNight = 1250,
            CleaningFee = 250,
            ServiceFee = 150,
            Address = "21 Loop Street",
            City = "Cape Town",
            PropertyType = "Apartment",
            HostId = host.Id,
            IsActive = true,
            CheckInRules = "Upload an ID or passport before arrival. Check-in is from 14:00.",
            Images =
            [
                new PropertyImage { ImageUrl = "/images/properties/bright-loft-1.png" },
                new PropertyImage { ImageUrl = "/images/properties/bright-loft-2.png" }
            ]
        };

        var cottage = new HostProperty
        {
            Title = "Quiet Garden Cottage",
            Description = "A private cottage near the city with a garden, secure parking and space for a small family.",
            PricePerNight = 890,
            CleaningFee = 180,
            ServiceFee = 110,
            Address = "8 Jacaranda Avenue",
            City = "Johannesburg",
            PropertyType = "Cottage",
            HostId = host.Id,
            IsActive = true,
            CheckInRules = "Bring the same identity document used during online check-in.",
            Images =
            [
                new PropertyImage { ImageUrl = "/images/properties/brick-apartment-1.png" },
                new PropertyImage { ImageUrl = "/images/properties/brick-apartment-2.png" }
            ]
        };

        var pendingProperty = new HostProperty
        {
            Title = "Durban Beach Studio",
            Description = "A compact studio within walking distance of the beach and local restaurants.",
            PricePerNight = 720,
            CleaningFee = 140,
            ServiceFee = 90,
            Address = "15 Marine Parade",
            City = "Durban",
            PropertyType = "Studio",
            HostId = applicant.Id,
            IsActive = false,
            Images = [new PropertyImage { ImageUrl = "/images/properties/ocean-apartment-1.png" }]
        };

        var oceanApartment = CreateOceanApartment(host.Id);
        var modernApartment = CreateModernApartment(host.Id);

        db.HostProperties.AddRange(loft, cottage, pendingProperty, oceanApartment, modernApartment);
        await db.SaveChangesAsync();

        // The applicant's property stays inactive until this pending application is approved.
        db.HostApplications.Add(new HostApplication
        {
            ApplicantId = applicant.Id,
            PropertyId = pendingProperty.Id,
            Status = ApplicationStatus.Pending
        });

        // Seed one pending and one approved booking for the Host and Guest dashboards.
        db.Bookings.AddRange(
            new Booking
            {
                PropertyId = loft.Id,
                GuestId = guest.Id,
                CheckInDate = DateTime.Today.AddDays(14),
                CheckOutDate = DateTime.Today.AddDays(17),
                NumberOfTravellers = 2,
                TotalPrice = (loft.PricePerNight * 3) + loft.CleaningFee + loft.ServiceFee,
                Status = BookingStatus.Pending
            },
            new Booking
            {
                PropertyId = cottage.Id,
                GuestId = guest.Id,
                CheckInDate = DateTime.Today.AddDays(30),
                CheckOutDate = DateTime.Today.AddDays(32),
                NumberOfTravellers = 1,
                TotalPrice = (cottage.PricePerNight * 2) + cottage.CleaningFee + cottage.ServiceFee,
                Status = BookingStatus.Approved
            });

        db.Messages.Add(new Message
        {
            SenderId = host.Id,
            RecipientId = guest.Id,
            Subject = "Welcome to Staybnb",
            Body = "Hello Grace, please use Messages if you have any questions about your stay."
        });

        db.Notifications.AddRange(
            new Notification
            {
                UserId = host.Id,
                Title = "New booking request",
                Body = "A guest requested Table Mountain Loft.",
                Type = "Booking",
                Link = "/HostDashboard"
            },
            new Notification
            {
                UserId = guest.Id,
                Title = "Booking approved",
                Body = "Your Quiet Garden Cottage booking is ready for check-in.",
                Type = "Booking",
                Link = "/GuestDashboard"
            });

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = superAdmin.Id,
            Action = "SystemSeeded",
            Details = "Roles, demonstration users and example listings were created."
        });

        await db.SaveChangesAsync();
    }

    // Replaces only the original SVG artwork and adds missing demonstration listings.
    private static async Task RefreshDemonstrationListingsAsync(ApplicationDbContext db, string hostId)
    {
        var listings = await db.HostProperties.Include(property => property.Images).ToListAsync();

        ReplacePlaceholderImages(
            db,
            listings.FirstOrDefault(property => property.Title == "Table Mountain Loft"),
            "/images/properties/bright-loft-1.png",
            "/images/properties/bright-loft-2.png");

        ReplacePlaceholderImages(
            db,
            listings.FirstOrDefault(property => property.Title == "Quiet Garden Cottage"),
            "/images/properties/brick-apartment-1.png",
            "/images/properties/brick-apartment-2.png");

        ReplacePlaceholderImages(
            db,
            listings.FirstOrDefault(property => property.Title == "Durban Beach Studio"),
            "/images/properties/ocean-apartment-1.png");

        if (!listings.Any(property => property.Title == "Sea Point Ocean Apartment"))
        {
            db.HostProperties.Add(CreateOceanApartment(hostId));
        }

        if (!listings.Any(property => property.Title == "Modern City Apartment"))
        {
            db.HostProperties.Add(CreateModernApartment(hostId));
        }

        await db.SaveChangesAsync();
    }

    private static void ReplacePlaceholderImages(
        ApplicationDbContext db,
        HostProperty? property,
        params string[] imageUrls)
    {
        if (property is null || !property.Images.Any(image => image.ImageUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        db.PropertyImages.RemoveRange(property.Images);
        property.Images = imageUrls.Select(imageUrl => new PropertyImage { ImageUrl = imageUrl }).ToList();
    }

    private static HostProperty CreateOceanApartment(string hostId) => new()
    {
        Title = "Sea Point Ocean Apartment",
        Description = "A comfortable ocean-facing apartment with a private balcony, open-plan living area and easy access to the promenade.",
        PricePerNight = 1650,
        CleaningFee = 300,
        ServiceFee = 180,
        Address = "34 Beach Road",
        City = "Cape Town",
        PropertyType = "Apartment",
        HostId = hostId,
        IsActive = true,
        CheckInRules = "Upload an ID or passport before arrival. Check-in is from 15:00.",
        Images =
        [
            new PropertyImage { ImageUrl = "/images/properties/ocean-apartment-1.png" },
            new PropertyImage { ImageUrl = "/images/properties/ocean-apartment-2.png" },
            new PropertyImage { ImageUrl = "/images/properties/ocean-apartment-3.png" }
        ]
    };

    private static HostProperty CreateModernApartment(string hostId) => new()
    {
        Title = "Modern City Apartment",
        Description = "A spacious modern apartment with an open living and dining area, large windows and a private outdoor space.",
        PricePerNight = 1420,
        CleaningFee = 260,
        ServiceFee = 160,
        Address = "12 Rosebank Avenue",
        City = "Johannesburg",
        PropertyType = "Apartment",
        HostId = hostId,
        IsActive = true,
        CheckInRules = "Upload an identity document before arrival. Check-in is from 14:00.",
        Images = [new PropertyImage { ImageUrl = "/images/properties/modern-apartment.png" }]
    };

    // Returns the existing account or creates it, then ensures the requested role is present.
    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
