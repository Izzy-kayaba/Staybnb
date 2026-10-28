// Defines the single database boundary and the relationships required by each workflow.
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Web.Models;

namespace Staybnb.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Each set represents a table used by the assessed workflows.
    public DbSet<HostProperty> HostProperties => Set<HostProperty>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<HostApplication> HostApplications => Set<HostApplication>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Money values keep two decimal places in SQL Server.
        builder.Entity<HostProperty>().Property(p => p.PricePerNight).HasPrecision(18, 2);
        builder.Entity<HostProperty>().Property(p => p.CleaningFee).HasPrecision(18, 2);
        builder.Entity<HostProperty>().Property(p => p.ServiceFee).HasPrecision(18, 2);
        builder.Entity<Booking>().Property(b => b.TotalPrice).HasPrecision(18, 2);
        builder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);

        // These relationships protect ownership while avoiding accidental user deletion cascades.
        builder.Entity<HostApplication>()
            .HasOne(a => a.Property).WithOne(p => p.Application)
            .HasForeignKey<HostApplication>(a => a.PropertyId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<HostApplication>()
            .HasOne(a => a.Applicant).WithMany()
            .HasForeignKey(a => a.ApplicantId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<HostProperty>()
            .HasOne(p => p.Host).WithMany(u => u.Properties)
            .HasForeignKey(p => p.HostId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Booking>()
            .HasOne(b => b.Guest).WithMany(u => u.Bookings)
            .HasForeignKey(b => b.GuestId).OnDelete(DeleteBehavior.Restrict);

        // Messages and verification records retain their users instead of cascading deletions.
        builder.Entity<Message>()
            .HasOne(m => m.Sender).WithMany()
            .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Message>()
            .HasOne(m => m.Recipient).WithMany()
            .HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<GuestDocument>()
            .HasOne(d => d.UploadedBy).WithMany()
            .HasForeignKey(d => d.UploadedById).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Review>()
            .HasOne(r => r.Reviewer).WithMany()
            .HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);

        // A property can appear only once in the same user's wishlist.
        builder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.PropertyId }).IsUnique();
        builder.Entity<WishlistItem>()
            .HasOne(w => w.User).WithMany()
            .HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Restrict);

        // The pair of identifiers uniquely represents one property-to-amenity assignment.
        builder.Entity<PropertyAmenity>().HasKey(pa => new { pa.PropertyId, pa.AmenityId });
    }
}
