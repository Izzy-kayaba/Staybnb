// Keeps the stored entities explicit so their keys and relationships are easy to trace.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staybnb.Web.Models;

public class HostProperty
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [Range(1, 1000000)] public decimal PricePerNight { get; set; }
    [Range(0, 1000000)] public decimal CleaningFee { get; set; }
    [Range(0, 1000000)] public decimal ServiceFee { get; set; }
    [Required, MaxLength(200)] public string Address { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string City { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string PropertyType { get; set; } = string.Empty;

    // Links the property to the host who owns or manages the listing.
    [Required] public string HostId { get; set; } = string.Empty;
    public ApplicationUser? Host { get; set; }

    public bool IsActive { get; set; }
    [MaxLength(2000)] public string CheckInRules { get; set; } = "Present the required document on arrival.";
    public DocumentType RequiredDocumentType { get; set; } = DocumentType.IdentityDocument;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Related images, bookings, and amenities associated with this property.
    public ICollection<PropertyImage> Images { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = [];

    // Optional application submitted for this property to become an active listing.
    public HostApplication? Application { get; set; }

    // Seeded listings have no application; submitted listings must be approved before activation.
    [NotMapped]
    public bool CanActivate => Application is null || Application.Status == ApplicationStatus.Approved;
}

public class PropertyImage
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }
    [Required, MaxLength(300)] public string ImageUrl { get; set; } = string.Empty;
}

// Tracks a host's submission of a property for approval.
public class HostApplication
{
    public int Id { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    public ApplicationUser? Applicant { get; set; }
    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }

    // Applications remain pending until reviewed and approved or rejected.
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }
    [MaxLength(500)] public string? RejectionReason { get; set; }
}

public class Booking
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }
    [Required] public string GuestId { get; set; } = string.Empty;
    public ApplicationUser? Guest { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    [Range(1, 20)] public int NumberOfTravellers { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Documents submitted by the guest for this booking.
    public ICollection<GuestDocument> Documents { get; set; } = [];

    // Optional payment record associated with the booking.
    public Payment? Payment { get; set; }
}

// Stores guest documents submitted for booking verification.
public class GuestDocument
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    [Required] public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser? UploadedBy { get; set; }
    public DocumentType DocumentType { get; set; }
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = string.Empty;

    // Identifies where the uploaded file is stored; access should be restricted to authorized users.
    [Required, MaxLength(300)] public string StoredFilePath { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedByHostId { get; set; }
}

// Represents messages exchanged between users.
public class Message
{
    public int Id { get; set; }
    [Required] public string SenderId { get; set; } = string.Empty;
    public ApplicationUser? Sender { get; set; }
    [Required] public string RecipientId { get; set; } = string.Empty;
    public ApplicationUser? Recipient { get; set; }
    [Required, MaxLength(120)] public string Subject { get; set; } = string.Empty;
    [Required, MaxLength(4000)] public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Optional reference to the original message for reply threading.
    public int? ReplyToMessageId { get; set; }
}

// Stores notifications intended for individual users.
public class Notification
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    [Required, MaxLength(120)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string Body { get; set; } = string.Empty;
    [MaxLength(40)] public string Type { get; set; } = "General";
    [MaxLength(300)] public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Records significant application actions for auditing and troubleshooting.
public class ActivityLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    [Required, MaxLength(80)] public string Action { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string Details { get; set; } = string.Empty;
    [MaxLength(64)] public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Represents a payment associated with a booking.
public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Stores a guest's rating and feedback for a property.
public class Review
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }
    [Required] public string ReviewerId { get; set; } = string.Empty;
    public ApplicationUser? Reviewer { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Defines amenities that can be assigned to multiple properties.
public class Amenity
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string? IconClass { get; set; }
    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = [];
}

// Join entity connecting properties and amenities in a many-to-many relationship.
public class PropertyAmenity
{
    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }
    public int AmenityId { get; set; }
    public Amenity? Amenity { get; set; }
}

// Records properties saved by users for later viewing.
public class WishlistItem
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
