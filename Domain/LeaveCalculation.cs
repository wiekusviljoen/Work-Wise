namespace LeaveWise.Domain;

/// <summary>Configurable calculation inputs. Values must come from a reviewed policy or confirmed contract; this type deliberately does not hard-code country law.</summary>
public sealed record LeaveCalculationPolicy(string PolicyCode, string CountryCode, int CycleMonths, decimal EntitlementDaysPerCycle, IReadOnlySet<DayOfWeek> ScheduledWorkDays);
public sealed record LeaveLedgerEntry(DateOnly EffectiveDate, decimal DayDelta, string EntryType, string Reference, string? Notes = null);
public sealed record LeaveCalculationResult(decimal StartingBalance, decimal AccruedDays, decimal UsedDays, decimal Adjustments, decimal ClosingBalance, int ScheduledWorkdaysInPeriod, string Explanation);

public static class LeaveCalculator
{
    public static int CountScheduledWorkdays(DateOnly startDate, DateOnly endDate, IReadOnlySet<DayOfWeek> scheduledWorkDays, IReadOnlySet<DateOnly>? holidays = null)
    {
        if (endDate < startDate) throw new ArgumentException("End date must be on or after start date.", nameof(endDate));
        ArgumentNullException.ThrowIfNull(scheduledWorkDays);
        if (scheduledWorkDays.Count == 0) throw new ArgumentException("At least one scheduled workday is required.", nameof(scheduledWorkDays));
        holidays ??= new HashSet<DateOnly>();
        var count = 0;
        for (var date = startDate; ; date = date.AddDays(1))
        {
            if (scheduledWorkDays.Contains(date.DayOfWeek) && !holidays.Contains(date)) count++;
            if (date == endDate) break; // Avoid overflowing DateOnly when the range ends at 9999-12-31.
        }
        return count;
    }

    public static decimal CalculateMonthlyAccrual(LeaveCalculationPolicy policy, int completedMonths)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.CycleMonths <= 0) throw new ArgumentOutOfRangeException(nameof(policy), "Cycle length must be positive.");
        if (policy.EntitlementDaysPerCycle < 0) throw new ArgumentOutOfRangeException(nameof(policy), "Entitlement cannot be negative.");
        if (completedMonths < 0) throw new ArgumentOutOfRangeException(nameof(completedMonths));
        var monthsInCycle = Math.Min(completedMonths, policy.CycleMonths);
        return decimal.Round(policy.EntitlementDaysPerCycle * monthsInCycle / policy.CycleMonths, 4, MidpointRounding.AwayFromZero);
    }

    public static LeaveCalculationResult CalculateBalance(decimal startingBalance, IEnumerable<LeaveLedgerEntry> entries, DateOnly periodStart, DateOnly periodEnd, IReadOnlySet<DayOfWeek> scheduledWorkDays, IReadOnlySet<DateOnly>? holidays = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var periodEntries = entries.Where(e => e.EffectiveDate >= periodStart && e.EffectiveDate <= periodEnd).ToArray();
        var accrued = periodEntries.Where(e => e.EntryType.Equals("accrual", StringComparison.OrdinalIgnoreCase)).Sum(e => e.DayDelta);
        var used = -periodEntries.Where(e => e.EntryType.Equals("usage", StringComparison.OrdinalIgnoreCase)).Sum(e => e.DayDelta);
        var adjustments = periodEntries.Where(e => !e.EntryType.Equals("accrual", StringComparison.OrdinalIgnoreCase) && !e.EntryType.Equals("usage", StringComparison.OrdinalIgnoreCase)).Sum(e => e.DayDelta);
        var workdays = CountScheduledWorkdays(periodStart, periodEnd, scheduledWorkDays, holidays);
        var closing = startingBalance + periodEntries.Sum(e => e.DayDelta);
        var explanation = $"Opening balance {startingBalance:0.####} + accruals {accrued:0.####} + all ledger movements {periodEntries.Sum(e => e.DayDelta):0.####} = closing balance {closing:0.####}. Counted {workdays} scheduled workday(s) in the selected period; holidays and non-working days are excluded.";
        return new LeaveCalculationResult(startingBalance, accrued, used, adjustments, closing, workdays, explanation);
    }
}
