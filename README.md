# LeaveWise

LeaveWise is an employment leave and benefits assistant for Namibia and South Africa. It is being built for employees and employers, with contract-assisted setup, transparent leave calculations, approval workflows and benefit reminders.

## Current prototype
- ASP.NET Core Blazor Web App on .NET 10.
- Individual Identity account scaffold.
- Responsive landing page and navigation.
- Early UI flows for leave requests, contract selection preview, benefits and employer team overview.
- Leave request interaction is currently in-memory demo behavior; it is not persisted or emailed.
- Contract selection is local filename display only; files are not uploaded or retained.
- Dashboard balances and benefits are sample data, not legal or payroll calculations.

## Product principles
1. AI extracts candidate facts; deterministic code calculates entitlements.
2. Contract clauses and statutory rules remain distinct.
3. Every active entitlement must be confirmed and traceable to its source.
4. Leave balance changes are ledger transactions, not silent overwrites.
5. Organization boundaries and least-privilege authorization apply server-side.
6. Salary impacts are estimates unless confirmed by payroll.
7. Health information is minimized.

## MVP implementation sequence
1. Identity and organization membership with company isolation.
2. Persistent employment profiles, work schedules and contract metadata.
3. Versioned Namibia and South Africa rule sets with approved reference tests.
4. Ledger-based leave balance calculations, requests and approvals.
5. Secure document storage and contract extraction/review.
6. Email delivery, reminders and audit history.
7. Salary-impact estimates and contract-specific bonus reminders.
8. PWA polish, accessibility, monitoring, backups and controlled pilot.

## Before production
- Configure verified email delivery; the generated identity email sender is not a production mail service.
- Use a production database and private object storage; do not ship the development SQLite database.
- Verify applicable legal rules, sector coverage, effective dates and edge cases with qualified reviewers.
- Implement privacy notices, consent, retention/deletion workflows, data processing agreements and incident response.
- Configure secrets outside source control, HTTPS, backups, restore drills and monitoring.
- Run security review and tenant-isolation tests.

## Local development
From the project directory, run dotnet restore, dotnet build and dotnet run.

Do not enter real employee information into the prototype until production-grade persistence, access controls, email verification, privacy controls and secure document handling are implemented.
