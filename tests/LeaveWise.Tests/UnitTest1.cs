using LeaveWise.Domain;

namespace LeaveWise.Tests;

public sealed class LeaveCalculatorTests
{
    private static readonly IReadOnlySet<DayOfWeek> Weekdays = new HashSet<DayOfWeek>
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    [Fact]
    public void CountScheduledWorkdays_CountsInclusiveWeekdaysAndExcludesHoliday()
    {
        var holiday = new HashSet<DateOnly> { new(2026, 10, 7) };
        var result = LeaveCalculator.CountScheduledWorkdays(new(2026, 10, 5), new(2026, 10, 9), Weekdays, holiday);
        Assert.Equal(4, result);
    }

    [Fact]
    public void CountScheduledWorkdays_RejectsReversedRange()
    {
        Assert.Throws<ArgumentException>(() =>
            LeaveCalculator.CountScheduledWorkdays(new(2026, 10, 10), new(2026, 10, 9), Weekdays));
    }

    [Fact]
    public void CalculateMonthlyAccrual_UsesConfiguredEntitlementAndCapsAtCycle()
    {
        var policy = new LeaveCalculationPolicy("TEST-01", "XX", 12, 24m, Weekdays);
        Assert.Equal(12m, LeaveCalculator.CalculateMonthlyAccrual(policy, 6));
        Assert.Equal(24m, LeaveCalculator.CalculateMonthlyAccrual(policy, 18));
    }

    [Fact]
    public void CalculateBalance_ReconstructsClosingBalanceFromLedger()
    {
        var entries = new[]
        {
            new LeaveLedgerEntry(new(2026, 10, 1), 2m, "accrual", "A-1"),
            new LeaveLedgerEntry(new(2026, 10, 5), -1.5m, "usage", "L-1"),
            new LeaveLedgerEntry(new(2026, 10, 6), 0.5m, "adjustment", "ADJ-1")
        };
        var result = LeaveCalculator.CalculateBalance(10m, entries, new(2026, 10, 1), new(2026, 10, 31), Weekdays);
        Assert.Equal(2m, result.AccruedDays);
        Assert.Equal(1.5m, result.UsedDays);
        Assert.Equal(0.5m, result.Adjustments);
        Assert.Equal(11m, result.ClosingBalance);
        Assert.Contains("closing balance 11", result.Explanation);
    }
}

public sealed class LeavePolicyCatalogTests
{
    [Fact]
    public void CatalogContainsBothCountriesAndEveryEntryRequiresLegalReview()
    {
        Assert.Contains(LeavePolicyCatalog.All, x => x.CountryCode == "NA");
        Assert.Contains(LeavePolicyCatalog.All, x => x.CountryCode == "ZA");
        Assert.All(LeavePolicyCatalog.All, x => Assert.True(x.RequiresLegalReview));
        Assert.All(LeavePolicyCatalog.All, x => Assert.False(string.IsNullOrWhiteSpace(x.SourceUrl)));
    }

    [Fact]
    public void NamibiaCatalogDistinguishesCompassionateLeaveFromSouthAfricanFamilyResponsibilityLeave()
    {
        Assert.Contains(LeavePolicyCatalog.ForCountry("NA"), x => x.LeaveType == "Compassionate leave");
        Assert.Contains(LeavePolicyCatalog.ForCountry("ZA"), x => x.LeaveType == "Family responsibility leave");
    }

    [Fact]
    public void SouthAfricanParentalLeaveCatalogReferencesConstitutionalCourtInterimFramework()
    {
        var rule = Assert.Single(LeavePolicyCatalog.ForCountry("ZA"), x => x.LeaveType == "Parental leave");
        Assert.Contains("2025", rule.LegalReference);
        Assert.Contains("interim", rule.BaselineSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(rule.RequiresLegalReview);
    }
}
