// Extends the Identity account with display details and domain relationships.
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Staybnb.Web.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(80)] public string FirstName { get; set; } = string.Empty;
    [MaxLength(80)] public string LastName { get; set; } = string.Empty;
    [MaxLength(300)] public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation collections connect the account to its owned listings and bookings.
    public ICollection<HostProperty> Properties { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];

    // Falls back to the email address when profile names have not been supplied.
    public string DisplayName => string.IsNullOrWhiteSpace(FirstName)
        ? Email ?? "Staybnb user"
        : $"{FirstName} {LastName}".Trim();
}
