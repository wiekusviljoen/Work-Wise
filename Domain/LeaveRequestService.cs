using System.Data;
using System.Text.Json;
using LeaveWise.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Domain;

public sealed record LeaveActionResult(bool Succeeded, string Message, LeaveRequestRecord? Request = null);

public static class LeaveRequestService
{
    public static async Task<LeaveActionResult> SubmitAsync(ApplicationDbContext db, string actor, Guid employmentId,
        string leaveType, DateOnly start, DateOnly end, string? note, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(actor)) return new(false, "Sign in before requesting leave.");
        if (start < today || end < start || end.DayNumber - start.DayNumber > 365)
            return new(false, "Choose today or future dates, in order, covering no more than 366 days.");
        if (!new[] { "Annual leave", "Sick leave", "Family responsibility leave", "Compassionate leave", "Maternity leave", "Parental leave", "Unpaid leave", "Other" }.Contains(leaveType) || note?.Length > 1000)
            return new(false, "Check the leave type and keep your note within 1,000 characters.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var employment = await db.EmploymentRecords.Include(x => x.Organization)
            .SingleOrDefaultAsync(x => x.Id == employmentId && x.EmployeeUserId == actor && x.Status == "Active");
        if (employment is null) return new(false, "Select an active employment record linked to your account.");
        if (start < employment.EmploymentStartDate || (employment.EmploymentEndDate is { } last && end > last))
            return new(false, "Leave must fall within your employment dates.");
        if (await db.LeaveRequests.AnyAsync(x => x.EmploymentRecordId == employmentId && (x.Status == "Pending" || x.Status == "Approved") && x.StartDate <= end && x.EndDate >= start))
            return new(false, "These dates overlap a pending or approved request. Cancel a pending request before replacing it.");
        var schedule = Enumerable.Range(0, 7).Where(day => (employment.ScheduledWorkdaysMask & (1 << day)) != 0)
            .Select(day => (DayOfWeek)((day + 1) % 7)).ToHashSet();
        if (schedule.Count == 0) return new(false, "Ask your employer to configure your work schedule.");
        var days = LeaveCalculator.CountScheduledWorkdays(start, end, schedule);
        if (days == 0) return new(false, "The dates contain no scheduled workdays.");
        var request = new LeaveRequestRecord { EmploymentRecord = employment, LeaveType = leaveType, StartDate = start, EndDate = end, ScheduledWorkdays = days, EmployeeNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim(), RequestedByUserId = actor, Status = "Pending" };
        db.LeaveRequests.Add(request);
        AddAudit(db, request, actor, "LeaveRequestSubmitted");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(true, "Leave request saved for employer review.", request);
    }

    public static async Task<LeaveActionResult> CancelAsync(ApplicationDbContext db, string actor, Guid id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await db.LeaveRequests.Include(x => x.EmploymentRecord).SingleOrDefaultAsync(x => x.Id == id);
        if (string.IsNullOrWhiteSpace(actor) || request is null || request.RequestedByUserId != actor || request.EmploymentRecord.EmployeeUserId != actor)
            return new(false, "You can only cancel your own leave requests.");
        if (request.Status != "Pending") return new(false, "Only pending requests can be cancelled. Refresh to see the latest decision.");
        request.Status = "Cancelled";
        request.DecisionUtc = DateTime.UtcNow;
        AddAudit(db, request, actor, "LeaveRequestCancelled");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(true, "Leave request cancelled.");
    }

    public static async Task<LeaveActionResult> DecideAsync(ApplicationDbContext db, string actor, Guid id, bool approve, decimal days, bool confirmed, DateOnly today)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await db.LeaveRequests.Include(x => x.EmploymentRecord).SingleOrDefaultAsync(x => x.Id == id);
        if (string.IsNullOrWhiteSpace(actor) || request is null || !await db.OrganizationMemberships.AnyAsync(x => x.OrganizationId == request.EmploymentRecord.OrganizationId && x.UserId == actor && x.Role == "Owner" && x.IsActive))
            return new(false, "Only an active owner of this workspace can decide the request.");
        if (request.Status != "Pending") return new(false, "This request is no longer pending. Refresh the list.");
        if (request.RequestedByUserId == actor) return new(false, "Another owner must review your own leave request.");
        if (approve)
        {
            if (!confirmed || days <= 0 || days > request.EndDate.DayNumber - request.StartDate.DayNumber + 1)
                return new(false, "Check the work schedule and holidays, enter a valid day count, and confirm it.");
            if (request.EmploymentRecord.Status != "Active" || request.StartDate < request.EmploymentRecord.EmploymentStartDate || (request.EmploymentRecord.EmploymentEndDate is { } last && request.EndDate > last))
                return new(false, "This employee is inactive or the request falls outside their employment dates.");
            if (await db.LeaveRequests.AnyAsync(x => x.Id != id && x.EmploymentRecordId == request.EmploymentRecordId && x.Status == "Approved" && x.StartDate <= request.EndDate && x.EndDate >= request.StartDate))
                return new(false, "This request overlaps approved leave.");
            if (request.LeaveType != "Unpaid leave")
            {
                var policy = await db.OrganizationLeavePolicies.Where(x => x.OrganizationId == request.EmploymentRecord.OrganizationId && x.LeaveType == request.LeaveType && (x.Status == "Active" || x.Status == "Superseded") && x.EffectiveFrom <= request.StartDate && (x.EffectiveTo == null || x.EffectiveTo >= request.StartDate)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync();
                if (policy is null) return new(false, "Configure a reviewed leave policy covering the requested dates before approving paid leave.");
                await LeaveLedgerService.SyncAccrualsAsync(db, request.EmploymentRecord, today, actor);
                var balance = await LeaveLedgerService.GetAvailableBalanceAsync(db, request.EmploymentRecordId, request.LeaveType, today);
                if (!policy.AllowNegativeBalance && balance < days) return new(false, $"Insufficient available balance: {balance:0.####} days, with future approved leave already reserved.");
                db.LeaveLedger.Add(new LeaveLedgerRecord { EmploymentRecordId = request.EmploymentRecordId, LeaveType = request.LeaveType, EffectiveDate = request.StartDate, DayDelta = -days, EntryType = "Usage", Reference = $"leave-request:{id:N}", CreatedByUserId = actor });
            }
            request.ScheduledWorkdays = days;
        }
        request.Status = approve ? "Approved" : "Rejected";
        request.ReviewedByUserId = actor;
        request.DecisionUtc = DateTime.UtcNow;
        AddAudit(db, request, actor, approve ? "LeaveRequestApproved" : "LeaveRequestRejected");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(true, approve ? "Leave approved. The employee can see the decision in request history." : "Leave declined. The employee can see the decision in request history.");
    }

    private static void AddAudit(ApplicationDbContext db, LeaveRequestRecord request, string actor, string action) =>
        db.AuditEvents.Add(new AuditEvent { OrganizationId = request.EmploymentRecord.OrganizationId, ActorUserId = actor, Action = action, EntityType = "LeaveRequest", EntityId = request.Id.ToString(), DetailsJson = JsonSerializer.Serialize(new { request.LeaveType, request.StartDate, request.EndDate, request.Status, request.ScheduledWorkdays }) });
}
