namespace LeaveWise.Services;

/// <summary>
/// Counts ordinary working days in an inclusive date range. This utility is
/// not a statutory entitlement engine: holidays, sector rules, contract terms,
/// eligibility and country-specific law must be applied separately.
/// </summary>
public sealed class LeaveCalculationService
{
    public int CountWorkingDays(DateTime startDate, DateTime endDate, int ordinaryDaysPerWeek = 5)
    {
        if (endDate.Date < startDate.Date)
            throw new ArgumentException("End date must be on or after start date.", nameof(endDate));
        if (ordinaryDaysPerWeek is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(ordinaryDaysPerWeek));

        var total = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var day = (int)date.DayOfWeek;
            var isWorkday = ordinaryDaysPerWeek switch
            {
                7 => true,
                6 => day != 0,
                5 => day is >= 1 and <= 5,
                4 => day is >= 1 and <= 4,
                3 => day is >= 1 and <= 3,
                2 => day is 1 or 2,
                1 => day == 1,
                _ => false
            };
            if (isWorkday) total++;
        }
        return total;
    }
}