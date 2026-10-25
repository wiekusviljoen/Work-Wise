namespace LeaveWise.Domain;

/// <summary>
/// Chooses a signed-in account's destination without trusting a stale, role-incompatible
/// ReturnUrl from a previous navigation attempt.
/// </summary>
public static class AccountLandingService
{
    public static string Resolve(AccountAccessService.Access access, string? requestedReturnUrl)
    {
        var landing = access.IsOwner ? "approvals"
            : access.CanUseEmployerWorkspace ? "workspace"
            : "leave";

        if (string.IsNullOrWhiteSpace(requestedReturnUrl))
            return landing;

        var requested = requestedReturnUrl.Trim();
        // Reject absolute, protocol-relative and malformed links. Never follow arbitrary
        // paths after authentication; only recognized in-app destinations are preserved.
        if (requested.StartsWith("//", StringComparison.Ordinal)
            || requested.Contains('\\')
            || !Uri.IsWellFormedUriString(requested, UriKind.Relative))
            return landing;

        var path = requested.Split('?', '#')[0].TrimStart('/');
        if (path.Length == 0)
            return landing;

        if (path.Equals("approvals", StringComparison.OrdinalIgnoreCase)
            || path.Equals("policies", StringComparison.OrdinalIgnoreCase))
            return access.IsOwner ? requested : landing;

        if (path.Equals("workspace", StringComparison.OrdinalIgnoreCase)
            || path.Equals("team", StringComparison.OrdinalIgnoreCase))
            return access.CanUseEmployerWorkspace ? requested : landing;

        if (path.Equals("leave", StringComparison.OrdinalIgnoreCase)
            || path.Equals("contracts", StringComparison.OrdinalIgnoreCase)
            || path.Equals("benefits", StringComparison.OrdinalIgnoreCase)
            || path.Equals("rules", StringComparison.OrdinalIgnoreCase)
            || path.Equals("Account/Manage", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Account/Manage/", StringComparison.OrdinalIgnoreCase))
            return requested;

        return landing;
    }
}
