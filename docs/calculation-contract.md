# Calculation contract

## Core safety rule
The calculator is policy-driven. Do not embed statutory annual or sick leave values in shared business logic. Country-specific policy data must be reviewed, versioned, dated, and tested before it can be enabled for production.

## Required inputs
- Country and legal-rule version
- Employment start date and any cycle dates
- Employee work schedule and hours
- Contractual entitlements and clause evidence
- Applicable public holidays and sector-specific terms
- Approved ledger entries and adjustments

## Expected behavior
- Date ranges are inclusive.
- Only scheduled workdays count.
- Public holidays are excluded when the applicable policy says they are excluded.
- Invalid ranges and empty work schedules are rejected.
- Accrual calculations use an explicitly supplied entitlement and cycle length.
- Every balance should be reconstructable from ledger entries.
- Estimates must show their inputs and assumptions.

## Production work still required
- Add effective-dated Namibia and South Africa statutory rule providers.
- Confirm rules with qualified reviewers and build golden reference cases.
- Model leave cycles, part-time schedules, variable shifts, unpaid periods, termination, carry-over, leave in hours, public holidays and sector-specific agreements.
- Define rounding and payroll conventions per employer/policy.
- Add property-based tests, boundary tests and independent reference calculations.
