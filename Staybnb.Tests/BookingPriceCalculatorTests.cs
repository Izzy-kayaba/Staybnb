// Verifies the exact price formula and its invalid-date boundary.
using Staybnb.Web.Services;

namespace Staybnb.Tests;

public class BookingPriceCalculatorTests
{
    [Fact]
    public void Calculate_AddsNightlyCleaningAndServiceFees()
    {
        var total = BookingPriceCalculator.Calculate(
            1000m,
            new DateTime(2026, 9, 10),
            new DateTime(2026, 9, 13),
            250m,
            150m);

        Assert.Equal(3400m, total);
    }

    [Fact]
    public void Calculate_UsesCalendarDaysAsNights()
    {
        var total = BookingPriceCalculator.Calculate(
            800m,
            new DateTime(2026, 9, 10, 16, 0, 0),
            new DateTime(2026, 9, 12, 9, 0, 0),
            100m,
            50m);

        Assert.Equal(1750m, total);
    }

    [Fact]
    public void Calculate_RejectsCheckoutOnOrBeforeCheckin()
    {
        Assert.Throws<ArgumentException>(() => BookingPriceCalculator.Calculate(
            1000m,
            new DateTime(2026, 9, 10),
            new DateTime(2026, 9, 10),
            250m,
            150m));
    }
}
