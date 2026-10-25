using LeaveWise.Services;
using Xunit;

namespace WorkWise.Tests;

public class LeaveCalculationServiceTests
{
    private readonly LeaveCalculationService _calculator = new();

    [Fact]
    public void MondayToFridayCountsFiveWorkingDays()
    {
        Assert.Equal(5, _calculator.CountWorkingDays(new DateTime(2026, 10, 12), new DateTime(2026, 10, 16), 5));
    }

    [Fact]
    public void WeekendOnlyRangeCountsZeroForFiveDaySchedule()
    {
        Assert.Equal(0, _calculator.CountWorkingDays(new DateTime(2026, 10, 10), new DateTime(2026, 10, 11), 5));
    }

    [Fact]
    public void SixDayScheduleIncludesSaturday()
    {
        Assert.Equal(6, _calculator.CountWorkingDays(new DateTime(2026, 10, 12), new DateTime(2026, 10, 17), 6));
    }

    [Fact]
    public void SingleWorkingDayRangeIsInclusive()
    {
        Assert.Equal(1, _calculator.CountWorkingDays(new DateTime(2026, 10, 12), new DateTime(2026, 10, 12), 5));
    }

    [Fact]
    public void ReversedRangeIsRejected()
    {
        Assert.Throws<ArgumentException>(() => _calculator.CountWorkingDays(new DateTime(2026, 10, 16), new DateTime(2026, 10, 12)));
    }

    [Fact]
    public void InvalidWorkweekIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.CountWorkingDays(new DateTime(2026, 10, 12), new DateTime(2026, 10, 12), 0));
    }
}