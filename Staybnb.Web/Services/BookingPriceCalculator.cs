// Keeps the assessed booking formula in one small, directly testable operation.
namespace Staybnb.Web.Services;

public static class BookingPriceCalculator
{
    // Uses calendar dates so partial arrival and departure days do not add extra nights.
    public static decimal Calculate(
        decimal pricePerNight,
        DateTime checkInDate,
        DateTime checkOutDate,
        decimal cleaningFee,
        decimal serviceFee)
    {
        var nights = (checkOutDate.Date - checkInDate.Date).Days;
        if (nights < 1)
        {
            throw new ArgumentException("Check-out must be after check-in.");
        }

        // The browser never supplies the final total; the server calculates it from stored rates.
        return (pricePerNight * nights) + cleaningFee + serviceFee;
    }
}
