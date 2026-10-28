// Named status values make workflow transitions readable in controllers and views.
namespace Staybnb.Web.Models;

// Describes the Admin decision for a submitted Host application.
public enum ApplicationStatus { Pending, Approved, Rejected }

// Tracks the booking from request through approval and arrival.
public enum BookingStatus { Pending, Approved, Rejected, CheckedIn, Cancelled }

// Limits check-in requirements to the two supported identity documents.
public enum DocumentType { IdentityDocument, Passport }

// Records the Host's decision about an uploaded check-in document.
public enum DocumentStatus { Pending, Verified, Rejected }

// Keeps the optional payment record ready for the states shown in the data design.
public enum PaymentStatus { Pending, Paid, Failed, Refunded }
