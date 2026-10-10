using Microsoft.EntityFrameworkCore;
using LeaveWise.Data;

namespace LeaveWise.Domain;

/// <summary>
/// Idempotent monthly accrual posting for employer-configured policies.
/// It does not determine statutory entitlement; only active, employer-reviewed policies are used.
/// </summary>
public static class LeaveLedgerService
{
    public static async Task<int> SyncAccrualsAsync(
        ApplicationDbContext db,
        EmploymentRecord employment,
        DateOnly throughDate,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(employment);
        if (string.IsNullOrWhiteSpace(actorUserId))
            throw new ArgumentException("An authenticated actor is required.", nameof(actorUserId));

        var policies = await db.OrganizationLeavePolicies
            .Where(p => p.OrganizationId == employment.OrganizationId
                && (p.Status == "Active" || p.Status == "Superseded")
                && p.EffectiveFrom <= throughDate)
            .OrderBy(p => p.EffectiveFrom)
            .ThenBy(p => p.Version)
            .ToListAsync(cancellationToken);

        var added = 0;
        foreach (var policy in policies)
        {
            if (policy.CycleMonths <= 0 || policy.EntitlementDaysPerCycle < 0
                || !string.Equals(policy.AccrualMethod, "MonthlyFixed", StringComparison.Ordinal))
                continue;

            var accrualBase = employment.EmploymentStartDate > policy.EffectiveFrom
                ? employment.EmploymentStartDate
                : policy.EffectiveFrom;
            var firstFullMonth = new DateOnly(accrualBase.Year, accrualBase.Month, 1);
            if (accrualBase.Day > 1)
                firstFullMonth = firstFullMonth.AddMonths(1);

            var roundedMonthlyDays = decimal.Round(
                policy.EntitlementDaysPerCycle / policy.CycleMonths,
                4,
                MidpointRounding.AwayFromZero);

            if (roundedMonthlyDays <= 0) continue;

            // Accrue for complete calendar months only. This avoids gaps when a policy version
            // changes on the first of a month and avoids granting a full month for a partial start month.
            for (var monthNumber = 0; monthNumber < 1200; monthNumber++)
            {
                DateOnly monthStart;
                try { monthStart = firstFullMonth.AddMonths(monthNumber); }
                catch (ArgumentOutOfRangeException) { break; }
                var accrualDate = new DateOnly(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

                if (accrualDate > throughDate) break;
                if (policy.EffectiveTo.HasValue && accrualDate > policy.EffectiveTo.Value) break;

                // Round monthly entries to four decimals but true-up the final month of each
                // cycle so rounding cannot create or destroy fractions of an entitlement day.
                var monthlyDays = monthNumber % policy.CycleMonths == policy.CycleMonths - 1
                    ? decimal.Round(policy.EntitlementDaysPerCycle - roundedMonthlyDays * (policy.CycleMonths - 1), 4, MidpointRounding.AwayFromZero)
                    : roundedMonthlyDays;
                var reference = $"accrual:{policy.Id:N}:{accrualDate:yyyyMMdd}";
                var exists = await db.LeaveLedger.AnyAsync(
                    x => x.EmploymentRecordId == employment.Id
                        && x.EntryType == "Accrual"
                        && x.Reference == reference,
                    cancellationToken);
                if (exists) continue;

                db.LeaveLedger.Add(new LeaveLedgerRecord
                {
                    EmploymentRecordId = employment.Id,
                    LeaveType = policy.LeaveType,
                    EffectiveDate = accrualDate,
                    DayDelta = monthlyDays,
                    EntryType = "Accrual",
                    Reference = reference,
                    Notes = $"Policy version {policy.Version}; {policy.AccrualMethod}; employer-configured policy {policy.Id}.",
                    CreatedByUserId = actorUserId
                });
                added++;
            }
        }

        if (added > 0)
        {
            db.AuditEvents.Add(new AuditEvent
            {
                OrganizationId = employment.OrganizationId,
                ActorUserId = actorUserId,
                Action = "LeaveAccrualsPosted",
                EntityType = "LeaveLedger",
                EntityId = employment.Id.ToString(),
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { EmploymentRecordId = employment.Id, AddedEntries = added, ThroughDate = throughDate })
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    public static async Task<decimal> GetBalanceAsync(
        ApplicationDbContext db,
        Guid employmentRecordId,
        string leaveType,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        return await db.LeaveLedger
            .Where(x => x.EmploymentRecordId == employmentRecordId
                && x.LeaveType == leaveType
                && x.EffectiveDate <= asOfDate)
            .SumAsync(x => (decimal?)x.DayDelta, cancellationToken) ?? 0m;
    }
}
