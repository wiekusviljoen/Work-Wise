using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeaveWise.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Domain;

public sealed record EmploymentInviteResult(bool Succeeded, string Message, string? Code = null);

/// <summary>
/// Issues short-lived, single-use employment invitations without requiring an email provider.
/// Only a SHA-256 hash is persisted; the raw code is shown to the employer once.
/// </summary>
public static class EmploymentInviteService
{
    private const string EntityType = "EmployeeInvite";
    private const int LifetimeDays = 7;

    public static async Task<EmploymentInviteResult> CreateInviteAsync(
        ApplicationDbContext db,
        Guid employmentRecordId,
        string actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (string.IsNullOrWhiteSpace(actorUserId))
            return new(false, "Sign in as an organization owner to create an invitation.");
        nowUtc = NormalizeUtc(nowUtc);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var employment = await db.EmploymentRecords
            .SingleOrDefaultAsync(x => x.Id == employmentRecordId, cancellationToken);
        if (employment is null || employment.Status != "Active")
            return new(false, "This active employment record could not be found.");

        var isOwner = await db.OrganizationMemberships.AnyAsync(x =>
            x.OrganizationId == employment.OrganizationId
            && x.UserId == actorUserId && x.IsActive && x.Role == "Owner", cancellationToken);
        if (!isOwner)
            return new(false, "Only an active organization owner can create an invitation.");
        if (!string.IsNullOrWhiteSpace(employment.EmployeeUserId))
            return new(false, "This employment record is already linked to an account.");

        var history = await db.AuditEvents
            .Where(x => x.OrganizationId == employment.OrganizationId
                && x.EntityType == EntityType
                && (x.Action == "EmployeeInviteCreated"
                    || x.Action == "EmployeeInviteRedeemed"
                    || x.Action == "EmployeeInviteRevoked"))
            .ToListAsync(cancellationToken);
        var inactiveIds = history
            .Where(x => x.Action is "EmployeeInviteRedeemed" or "EmployeeInviteRevoked")
            .Select(x => x.EntityId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var created in history.Where(x => x.Action == "EmployeeInviteCreated" && !inactiveIds.Contains(x.EntityId)))
        {
            var payload = DeserializePayload(created.DetailsJson);
            if (payload?.EmploymentRecordId == employmentRecordId)
            {
                db.AuditEvents.Add(new AuditEvent
                {
                    OrganizationId = employment.OrganizationId,
                    ActorUserId = actorUserId,
                    Action = "EmployeeInviteRevoked",
                    EntityType = EntityType,
                    EntityId = created.EntityId,
                    OccurredUtc = nowUtc,
                    DetailsJson = JsonSerializer.Serialize(new { Reason = "Replaced by a newer invitation." })
                });
            }
        }

        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var inviteId = Guid.NewGuid();
        var payloadJson = JsonSerializer.Serialize(new InvitePayload(
            employmentRecordId,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))),
            nowUtc.AddDays(LifetimeDays)));

        db.AuditEvents.Add(new AuditEvent
        {
            OrganizationId = employment.OrganizationId,
            ActorUserId = actorUserId,
            Action = "EmployeeInviteCreated",
            EntityType = EntityType,
            EntityId = inviteId.ToString("N"),
            OccurredUtc = nowUtc,
            DetailsJson = payloadJson
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, "Invitation created. Share this code directly with the intended employee.", code);
    }

    public static async Task<EmploymentInviteResult> RedeemInviteAsync(
        ApplicationDbContext db,
        string userId,
        string code,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        nowUtc = NormalizeUtc(nowUtc);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code) || code.Trim().Length > 128)
            return new(false, "The invitation code is invalid, expired, or already used.");

        var normalizedCode = code.Trim();
        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var account = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new { x.Email })
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(account?.Email))
            return new(false, "Your account needs an email address before it can accept an invitation.");

        // An invitation expires after seven days. Looking only at recent invitations bounds this scan.
        var history = await db.AuditEvents
            .Where(x => x.EntityType == EntityType
                && (x.Action == "EmployeeInviteCreated"
                    || x.Action == "EmployeeInviteRedeemed"
                    || x.Action == "EmployeeInviteRevoked")
                && x.OccurredUtc >= nowUtc.AddDays(-LifetimeDays))
            .ToListAsync(cancellationToken);
        var inactiveIds = history
            .Where(x => x.Action is "EmployeeInviteRedeemed" or "EmployeeInviteRevoked")
            .Select(x => x.EntityId)
            .ToHashSet(StringComparer.Ordinal);

        AuditEvent? matchingInvite = null;
        InvitePayload? matchingPayload = null;
        foreach (var created in history.Where(x => x.Action == "EmployeeInviteCreated" && !inactiveIds.Contains(x.EntityId)))
        {
            var payload = DeserializePayload(created.DetailsJson);
            if (payload is null || payload.ExpiresUtc <= nowUtc)
                continue;
            try
            {
                var storedHash = Convert.FromHexString(payload.TokenHash);
                if (storedHash.Length == candidateHash.Length
                    && CryptographicOperations.FixedTimeEquals(storedHash, candidateHash))
                {
                    matchingInvite = created;
                    matchingPayload = payload;
                    break;
                }
            }
            catch (FormatException)
            {
                // Ignore malformed historical records; never let them prevent other invitations from working.
            }
        }

        if (matchingInvite is null || matchingPayload is null)
            return new(false, "The invitation code is invalid, expired, or already used.");

        var employment = await db.EmploymentRecords
            .SingleOrDefaultAsync(x => x.Id == matchingPayload.EmploymentRecordId, cancellationToken);
        if (employment is null || employment.Status != "Active"
            || !string.Equals(employment.EmployeeEmail.Trim(), account.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return new(false, "The invitation code is invalid for this account or employment record.");

        // Conditional update prevents two simultaneous submissions from claiming the same record.
        var linked = await db.EmploymentRecords
            .Where(x => x.Id == employment.Id && x.Status == "Active"
                && x.EmployeeUserId == null
                && x.EmployeeEmail.ToLower() == account.Email.Trim().ToLower())
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.EmployeeUserId, userId), cancellationToken);
        if (linked != 1)
            return new(false, "This employment record has already been linked or is no longer active.");

        var membership = await db.OrganizationMemberships
            .SingleOrDefaultAsync(x => x.OrganizationId == employment.OrganizationId && x.UserId == userId, cancellationToken);
        if (membership is null)
        {
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrganizationId = employment.OrganizationId,
                UserId = userId,
                Role = "Employee",
                IsActive = true
            });
        }
        else if (membership.Role != "Owner" && !membership.IsActive)
        {
            membership.Role = "Employee";
            membership.IsActive = true;
        }

        db.AuditEvents.Add(new AuditEvent
        {
            OrganizationId = employment.OrganizationId,
            ActorUserId = userId,
            Action = "EmployeeInviteRedeemed",
            EntityType = EntityType,
            EntityId = matchingInvite.EntityId,
            OccurredUtc = nowUtc,
            DetailsJson = JsonSerializer.Serialize(new { EmploymentRecordId = employment.Id })
        });

        // Revoke any other outstanding codes for this record after a successful claim.
        foreach (var created in history.Where(x => x.Action == "EmployeeInviteCreated" && !inactiveIds.Contains(x.EntityId)
            && x.EntityId != matchingInvite.EntityId))
        {
            var payload = DeserializePayload(created.DetailsJson);
            if (payload?.EmploymentRecordId == employment.Id)
            {
                db.AuditEvents.Add(new AuditEvent
                {
                    OrganizationId = employment.OrganizationId,
                    ActorUserId = userId,
                    Action = "EmployeeInviteRevoked",
                    EntityType = EntityType,
                    EntityId = created.EntityId,
                    OccurredUtc = nowUtc,
                    DetailsJson = JsonSerializer.Serialize(new { Reason = "Employment record has been claimed." })
                });
            }
        }

        db.AuditEvents.Add(new AuditEvent
        {
            OrganizationId = employment.OrganizationId,
            ActorUserId = userId,
            Action = "EmploymentRecordLinked",
            EntityType = "EmploymentRecord",
            EntityId = employment.Id.ToString(),
            OccurredUtc = nowUtc,
            DetailsJson = JsonSerializer.Serialize(new { UserId = userId })
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, "Employment record linked successfully. You can now submit leave requests.");
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static InvitePayload? DeserializePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<InvitePayload>(json); }
        catch (JsonException) { return null; }
    }

    private sealed record InvitePayload(Guid EmploymentRecordId, string TokenHash, DateTime ExpiresUtc);
}
