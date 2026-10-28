// Proves that a Host application cannot pass validation without its required image.
using Staybnb.Web.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace Staybnb.Tests;

public class HostApplicationValidationTests
{
    [Fact]
    public void ApplicationWithoutImage_IsInvalid()
    {
        var model = new HostApplicationViewModel
        {
            Title = "Student Flat",
            Description = "A complete description",
            PricePerNight = 600,
            Address = "1 Main Road",
            City = "Pretoria",
            PropertyType = "Apartment",
            PropertyImage = null
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.PropertyImage)));
    }
}
