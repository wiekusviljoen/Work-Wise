# Work-Wise

Work-Wise (working product name: LeaveWise) is an employment leave and benefits assistant for Namibia and South Africa. The current early-access build focuses on company workspaces, employee employment records, persistent leave requests, owner approvals and an audit trail.

## Current implementation
- ASP.NET Core Blazor Web App on .NET 10 with ASP.NET Core Identity.
- Responsive landing page and navigation.
- SQLite persistence with Entity Framework Core migrations.
- Company workspace creation and owner membership.
- Employer-created employment records with country, start date, email and basic work schedule.
- A signed-in employee can link an unclaimed employment record by signing in with the same email address and submit leave requests.
- Leave requests are stored in the database, scheduled workdays are counted from the recorded weekday schedule, and request submissions/decisions create audit events.
- Organization owners can approve or decline pending requests. A user cannot decide their own request.
- Leave request decisions do not change leave balances yet; policy and entitlement rules must be reviewed before ledger movements are enabled.
- Contract selection is a local filename preview only. Files are not uploaded or retained.
- Dashboard balances and benefits are sample data, not legal or payroll calculations.

## Local development
Requirements: .NET 10 SDK.

From the repository directory:
```powershell
dotnet restore
dotnet test .\LeaveWise.sln
dotnet run
```

In Development, the application applies checked-in EF Core migrations at startup to the local SQLite database configured in `appsettings.json` (`Data/app.db`). Production deployments should apply reviewed migrations as a release step rather than relying on development startup behavior.

## Product principles
1. AI may extract candidate facts; deterministic code calculates entitlements.
2. Contract clauses and statutory rules remain distinct.
3. Every active entitlement must be confirmed and traceable to its source.
4. Leave balance changes must be ledger transactions, not silent overwrites.
5. Organization boundaries and least-privilege authorization apply server-side.
6. Salary impacts are estimates unless confirmed by payroll.
7. Health information is minimized.

## Next implementation priorities
1. Add explicit employee invitation/account-linking and employer role management.
2. Implement versioned Namibia and South Africa rule sets with qualified review and effective-date tests.
3. Add reviewed leave entitlements and ledger-based balances; never infer legal balances from demo values.
4. Add secure document storage, contract extraction and clause review.
5. Configure verified email delivery, reminders and notification preferences.
6. Add privacy notices, retention/deletion workflows, monitoring, backups and restore drills.
7. Add tenant-isolation, authorization and integration tests before a controlled pilot.

## Before production
- Configure verified email delivery; the generated Identity email sender is not a production mail service.
- Use a production database and private object storage; do not ship the development SQLite database.
- Verify legal rules, sector coverage, effective dates and edge cases with qualified reviewers.
- Implement privacy notices, consent, retention/deletion workflows, data processing agreements and incident response.
- Configure secrets outside source control, HTTPS, backups, restore drills and monitoring.
- Run a security review and tenant-isolation tests.

**Do not enter real employee information into this prototype until production-grade access controls, privacy controls, email verification and secure document handling have been reviewed.**
