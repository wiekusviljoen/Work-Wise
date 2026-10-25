# Work-Wise

Work-Wise is an employment leave and benefits assistant for employees and employers in Namibia and South Africa.

## Current development status

This repository contains an early ASP.NET Core / Blazor foundation:
- Responsive Work-Wise workspace and navigation
- ASP.NET Core Identity starter authentication
- Working-day preview utility
- Leave request form with session-only draft examples
- Contract file-selection UI (no upload or AI processing yet)
- Illustrative unpaid-leave estimate and bonus tracker explanation

**Important:** Dashboard balances are sample data. The current leave form creates in-memory drafts, not persisted or emailed requests. Contract file selection does not upload a file. The salary calculator is illustrative and does not establish legal or payroll correctness. Do not use this preview for official employment decisions.

## Technology

- .NET 10 / ASP.NET Core Blazor Web App
- ASP.NET Core Identity
- Entity Framework Core with SQLite starter database
- Responsive CSS

## Run locally

1. Install the .NET 10 SDK.
2. Open WorkWise.csproj in Visual Studio or a terminal.
3. Run `dotnet restore`.
4. Run `dotnet run`.
5. Open the HTTPS URL printed in the terminal.

## Roadmap

1. Domain model and persistent employment records
2. Organization membership and server-side tenant isolation
3. Versioned Namibia and South Africa leave policies, reviewed against official legislation
4. Persisted leave requests, approval workflow, audit events and email delivery
5. Secure contract storage, OCR, structured extraction and human confirmation
6. Contract-based benefit conditions and transparent payroll estimates
7. Integration tests, security review, backups and controlled pilot

## Security and legal notes

- Never store production credentials in source control.
- Configure real email delivery before enabling confirmed-account workflows.
- Use private document storage and explicit retention controls before accepting real contracts.
- AI extraction must never directly set official balances or authorize payroll changes.
- Country-specific statutory rules require legal verification, effective dates and reference test cases before activation.

## Repository layout

- Components/Pages: Blazor pages
- Components/Layout: shell and navigation
- Services: application services and calculation utilities
- Data: Identity and persistence
