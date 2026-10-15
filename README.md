# Work-Wise

Work-Wise is an employment leave and benefits assistant for Namibia and South Africa. The current early-access build focuses on company workspaces, employee employment records, persistent leave requests, owner approvals and an audit trail.

## Current implementation
- ASP.NET Core Blazor Web App on .NET 10 with ASP.NET Core Identity.
- Responsive landing page and navigation.
- SQLite persistence for local development (schema initialized with `EnsureCreated`) and SQL Server support for reviewed production migrations.
- Company workspace creation and owner membership.
- Employer-created employment records with country, start date, email and basic work schedule.
- An employer can issue a seven-day, single-use invitation code for an unlinked employment record. The code is stored only as a SHA-256 hash, is revoked when replaced, and can be redeemed only by a signed-in account whose email matches the employment record. A confirmed Identity email can also be linked automatically.
- Leave requests are stored in the database, scheduled workdays are counted from the recorded weekday schedule, and request submissions/decisions create audit events.
- Organization owners can approve or decline pending requests. A user cannot decide their own request, approved requests cannot overlap, and the approver must confirm the leave-day count after checking the work schedule and public holidays.
- Organization owners can create versioned, employer-configured leave policies with future effective dates, a review/source reference and an explicit activation attestation.
- The ledger service posts fixed monthly accruals idempotently for complete calendar months, respecting policy effective dates and employment start dates. Approved paid leave creates a usage entry in the same database transaction as the decision. Balance views are reconstructed from ledger entries.
- Approval requires a policy effective on the leave start date and sufficient balance unless that policy explicitly allows a negative balance. Unpaid leave does not deduct an accrued balance.
- The accrual engine currently supports fixed monthly accrual only. Public-holiday calendars, shift-specific patterns, day/hour-based statutory accrual, carry-over caps, termination calculations and retroactive policy corrections are not automated.
- A source-linked Namibia/South Africa statutory leave catalog is available, including the South African Constitutional Court's interim parental-leave framework. Every catalog entry is explicitly marked for legal review and is not itself used to calculate balances.
- Contract selection is a local filename preview only. Files are not uploaded or retained.
- Dashboard balances and benefits are sample data, not legal or payroll calculations.

## Local development
Requirements: .NET 10 SDK.

From the repository directory:
```powershell
dotnet restore
dotnet test .\Work-Wise.sln
dotnet run
```

In Development, the application initializes the local SQLite schema with `EnsureCreated` using the connection string in `appsettings.json` (`Data/workwise-dev.db`). This does not apply migrations to an existing database. Production SQL Server migrations are enabled only when `ApplyDatabaseMigrations=true`; review and back up the production database before applying schema changes.

## Product principles
1. AI may extract candidate facts; deterministic code calculates entitlements.
2. Contract clauses and statutory rules remain distinct.
3. Every active entitlement must be confirmed and traceable to its source.
4. Leave balance changes must be ledger transactions, not silent overwrites.
5. Organization boundaries and least-privilege authorization apply server-side.
6. Salary impacts are estimates unless confirmed by payroll.
7. Health information is minimized.

## Next implementation priorities
1. Add email delivery, invitation delivery and employer role management; the current single-use invitation code must be shared securely out-of-band.
2. Add public-holiday calendars and country/region-aware schedule rules; require explicit review when a calendar is missing.
3. Implement reviewed day/hour-based accrual formulas, carry-over rules, termination calculations and safe retroactive corrections.
4. Add contract-based entitlement comparison and qualified review workflows before enabling legal rule automation.
5. Add secure document storage, contract extraction and clause review.
6. Configure verified email delivery, reminders and notification preferences.
7. Add privacy notices, retention/deletion workflows, monitoring, backups, restore drills and deeper tenant-isolation tests before a controlled pilot.

## Before production
- Configure verified email delivery; the generated Identity email sender is not a production mail service.
- Use a production database and private object storage; do not ship the development SQLite database.
- Verify legal rules, sector coverage, effective dates and edge cases with qualified reviewers.
- Implement privacy notices, consent, retention/deletion workflows, data processing agreements and incident response.
- Configure secrets outside source control, HTTPS, backups, restore drills and monitoring.
- Run a security review and tenant-isolation tests.

**Do not enter real employee information into this prototype until production-grade access controls, privacy controls, email verification and secure document handling have been reviewed.**
