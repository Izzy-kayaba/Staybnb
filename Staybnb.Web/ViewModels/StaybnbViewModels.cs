// Defines only the values each form or dashboard is allowed to exchange with the browser.
using Microsoft.AspNetCore.Http;
using Staybnb.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace Staybnb.Web.ViewModels;

// Accepts the identity details required to create a new Guest account.
public class RegisterViewModel
{
    [Required, Display(Name = "First name")] public string FirstName { get; set; } = string.Empty;
    [Required, Display(Name = "Last name")] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Accepts credentials without exposing the complete stored user record.
public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
}

// Accepts the property details and image required for Admin review.
public class HostApplicationViewModel
{
    [Required, MaxLength(100)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, Range(1, 1000000), Display(Name = "Price per night")] public decimal PricePerNight { get; set; }
    [Range(0, 1000000), Display(Name = "Cleaning fee")] public decimal CleaningFee { get; set; }
    [Range(0, 1000000), Display(Name = "Service fee")] public decimal ServiceFee { get; set; }
    [Required, MaxLength(200)] public string Address { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string City { get; set; } = string.Empty;
    [Required, Display(Name = "Property type")] public string PropertyType { get; set; } = string.Empty;
    [Required, Display(Name = "Property image")] public IFormFile? PropertyImage { get; set; }
}

// Combines trusted display prices with the dates and traveller count entered by the Guest.
public class BookPropertyViewModel
{
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Check-in date")] public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);
    [Required, DataType(DataType.Date), Display(Name = "Check-out date")] public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);
    [Range(1, 20), Display(Name = "Number of travellers")] public int NumberOfTravellers { get; set; } = 1;
}

// Limits Host edits to the listing fields that may be changed from the dashboard.
public class EditPropertyViewModel
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [Range(1, 1000000), Display(Name = "Price per night")] public decimal PricePerNight { get; set; }
    [Range(0, 1000000), Display(Name = "Cleaning fee")] public decimal CleaningFee { get; set; }
    [Range(0, 1000000), Display(Name = "Service fee")] public decimal ServiceFee { get; set; }
    [Required] public string Address { get; set; } = string.Empty;
    [Required] public string City { get; set; } = string.Empty;
    [Required, Display(Name = "Property type")] public string PropertyType { get; set; } = string.Empty;
}

// Carries the booking identifier and one required identity document.
public class CheckInUploadViewModel
{
    public int BookingId { get; set; }
    public DocumentType DocumentType { get; set; }
    [Required, Display(Name = "ID or passport file")] public IFormFile? Document { get; set; }
}

// Accepts one message and optionally links it to the message being answered.
public class ComposeMessageViewModel
{
    [Required, EmailAddress, Display(Name = "Recipient email")] public string RecipientEmail { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Subject { get; set; } = string.Empty;
    [Required, MaxLength(4000)] public string Body { get; set; } = string.Empty;
    public int? ReplyToMessageId { get; set; }
}

// Groups the three collections displayed together on the Host dashboard.
public class HostDashboardViewModel
{
    public List<HostProperty> Properties { get; set; } = [];
    public List<Booking> PendingBookings { get; set; } = [];
    public List<GuestDocument> PendingDocuments { get; set; } = [];
}

// Groups application review, audit activity, and SuperAdmin promotion data.
public class AdminDashboardViewModel
{
    public List<HostApplication> PendingApplications { get; set; } = [];
    public List<ActivityLog> RecentActivity { get; set; } = [];
    public List<ApplicationUser> Guests { get; set; } = [];
    public bool IsSuperAdmin { get; set; }
}
