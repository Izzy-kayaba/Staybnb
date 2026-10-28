// Carries the request identifier shown when the production error page is used.
namespace Staybnb.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    // The error page hides the request section when no trace identifier is available.
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
