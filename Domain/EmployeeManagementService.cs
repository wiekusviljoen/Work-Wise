using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using LeaveWise.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Domain;

public sealed class EmployeeEditInput
{
    [Required, StringLength(160, MinimumLength = 2)] public string EmployeeName { get; set; } = "";
    [Required, EmailAddress, StringLength(320)] public string EmployeeEmail { get; set; } = "";
    public DateOnly EmploymentStartDate { get; set; }
    [Range(1, 168)] public decimal WeeklyHours { get; set; }
}

public static class EmployeeManagementService
{
    public static async Task<string?> SaveAsync(ApplicationDbContext db, Guid id, string actor, EmployeeEditInput? input)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var record = await db.EmploymentRecords.SingleOrDefaultAsync(x => x.Id == id);
        if (string.IsNullOrWhiteSpace(actor) || record is null || !await db.OrganizationMemberships.AnyAsync(x =>
            x.OrganizationId == record.OrganizationId && x.UserId == actor && x.IsActive && x.Role == "Owner"))
            return "Only an active owner of this workspace can change this employee.";
        if (record.Status != "Active") return "This employee is no longer active. Refresh the list.";
        if (input is not null)
        {
            input.EmployeeName = input.EmployeeName.Trim();
            input.EmployeeEmail = input.EmployeeEmail.Trim().ToLowerInvariant();
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
                return errors[0].ErrorMessage;
            if (input.EmploymentStartDate == default) return "Enter a valid employment start date.";
            if (record.EmployeeUserId is not null && !string.Equals(record.EmployeeEmail, input.EmployeeEmail, StringComparison.OrdinalIgnoreCase))
                return "A linked employee must change their email through their own account settings.";
            if (await db.EmploymentRecords.AnyAsync(x => x.Id != id && x.OrganizationId == record.OrganizationId && x.Status == "Active" && x.EmployeeEmail.ToLower() == input.EmployeeEmail))
                return "An active employee already uses this email in this workspace.";
            if (await db.OrganizationMemberships.Where(x => x.OrganizationId == record.OrganizationId && x.IsActive && x.Role == "Owner")
                .Join(db.Users, x => x.UserId, x => x.Id, (m, u) => u.Email).AnyAsync(x => x != null && x.ToLower() == input.EmployeeEmail))
                return "Use the employee's own email, not an employer account address.";
            if (input.EmploymentStartDate != record.EmploymentStartDate &&
                (await db.LeaveRequests.AnyAsync(x => x.EmploymentRecordId == id) || await db.LeaveLedger.AnyAsync(x => x.EmploymentRecordId == id)))
                return "The start date cannot change once leave history exists.";
            record.EmployeeName = input.EmployeeName;
            record.EmployeeEmail = input.EmployeeEmail;
            record.EmploymentStartDate = input.EmploymentStartDate;
            record.WeeklyHours = input.WeeklyHours;
        }
        else
        {
            record.Status = "Archived";
            if (record.EmployeeUserId is not null && !await db.EmploymentRecords.AnyAsync(x => x.Id != id && x.OrganizationId == record.OrganizationId && x.EmployeeUserId == record.EmployeeUserId && x.Status == "Active"))
            {
                var membership = await db.OrganizationMemberships.SingleOrDefaultAsync(x => x.OrganizationId == record.OrganizationId && x.UserId == record.EmployeeUserId);
                if (membership is not null && membership.Role == "Employee") membership.IsActive = false;
            }
        }
        // Invalidate outstanding invitations after either an edit or removal.
        var invitations = await db.AuditEvents.Where(x => x.OrganizationId == record.OrganizationId && x.EntityType == "EmployeeInvite" && x.Action == "EmployeeInviteCreated").ToListAsync();
        foreach (var invite in invitations)
        {
            using var payload = JsonDocument.Parse(invite.DetailsJson ?? "{}");
            if (payload.RootElement.TryGetProperty("EmploymentRecordId", out var value) && value.TryGetGuid(out var employeeId) && employeeId == id &&
                !await db.AuditEvents.AnyAsync(x => x.EntityType == "EmployeeInvite" && x.EntityId == invite.EntityId && (x.Action == "EmployeeInviteRevoked" || x.Action == "EmployeeInviteRedeemed")))
                db.AuditEvents.Add(new AuditEvent { OrganizationId = record.OrganizationId, ActorUserId = actor, EntityType = "EmployeeInvite", EntityId = invite.EntityId, Action = "EmployeeInviteRevoked" });
        }
        db.AuditEvents.Add(new AuditEvent { OrganizationId = record.OrganizationId, ActorUserId = actor, EntityType = "EmploymentRecord", EntityId = id.ToString(), Action = input is null ? "EmploymentRecordArchived" : "EmploymentRecordUpdated", DetailsJson = JsonSerializer.Serialize(new { record.EmployeeName, record.EmployeeEmail, record.EmploymentStartDate, record.WeeklyHours, record.Status }) });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }
}
