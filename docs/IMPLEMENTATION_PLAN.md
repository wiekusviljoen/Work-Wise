# Work-Wise implementation plan

## Phase 1 — secure foundations
- [ ] Confirm repository settings and branch protection
- [ ] Separate Identity user, organization membership and employee profile
- [ ] Require server-side organization authorization for all employer-owned data
- [ ] Add persistent leave request and transaction records
- [ ] Add integration tests for account and tenant isolation

## Phase 2 — reliable leave rules
- [ ] Define country policy schemas with source, effective date and version
- [ ] Verify Namibia rules against current official law and relevant sector agreements
- [ ] Verify South Africa rules against current official law and applicable sector agreements
- [ ] Add work schedules, leave cycles, accrual rules, holidays and special cases
- [ ] Create approved reference cases for each rule
- [ ] Show the inputs and rule version behind every result

## Phase 3 — contract workflow
- [ ] Private object storage and strict upload validation
- [ ] Malware scanning and file size/page limits
- [ ] OCR and AI schema extraction with page-level evidence
- [ ] Confidence flags, conflict detection and manual review
- [ ] User confirmation and immutable source document versions
- [ ] Retention, deletion and access audit

## Phase 4 — leave and communications
- [ ] Persist requests and prevent duplicate submissions
- [ ] Manager approval/rejection with reasons and history
- [ ] Transactional email provider and verified sender domain
- [ ] Delivery status, retries, bounce handling and reminder preferences
- [ ] Audit events for balance adjustments and approvals

## Phase 5 — salary and benefits
- [ ] Employer-configured salary deduction basis
- [ ] Explicit estimate inputs and calculation explanation
- [ ] Bonus clauses, eligibility conditions and pro-rating
- [ ] Reminder scheduling and timezone handling
- [ ] Clearly separate contract entitlement, statutory entitlement, estimate and actual payroll

## Release gates
- [ ] All enabled legal-rule reference cases pass
- [ ] No known critical/high security findings
- [ ] Tenant-isolation tests pass
- [ ] Backup restoration tested
- [ ] Email delivery and failure modes tested
- [ ] Mobile usability tested
- [ ] Pilot employers confirm the workflow is useful
