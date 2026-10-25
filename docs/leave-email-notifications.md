# Employer leave-request email notifications

When a linked employee submits leave, Work-Wise saves the pending request and attempts to email active organization owners with confirmed account email addresses. Owners must sign in and use **Leave approvals** to approve or decline; an email reply cannot make a decision.

Configure these environment variables on the host (or .NET user-secrets locally):

- `Email__SmtpHost`: SMTP server host
- `Email__SmtpPort`: SMTP port, default 587
- `Email__From`: verified sender address
- `Email__SmtpUsername`: SMTP login (if required)
- `Email__SmtpPassword`: SMTP password or provider app password (if required)

Use an SMTP provider supporting TLS on the selected port. Never commit SMTP credentials to Git. The existing Identity registration/password-reset sender is still a no-op and must be configured separately before enabling required email confirmation.

If SMTP is missing, delivery fails, or the organization has no active owner with a confirmed email address, the request is still saved and the employee is informed that email was not delivered. The owner can review pending requests in Work-Wise. This first implementation does not retry unsuccessful deliveries or track provider-side delivery confirmations.
