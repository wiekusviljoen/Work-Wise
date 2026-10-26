using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using LeaveWise.Data;
using Microsoft.AspNetCore.Identity;

namespace LeaveWise.Components.Account;

internal sealed class IdentitySmtpEmailSender(IConfiguration configuration) : IEmailSender<ApplicationUser>
{
    public static bool IsConfigured(IConfiguration configuration) =>
        new[] { "Email:SmtpHost", "Email:From", "Email:SmtpUsername", "Email:SmtpPassword" }
            .All(key => !string.IsNullOrWhiteSpace(configuration[key]));

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Work Wise email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Work Wise password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your Work Wise password", $"Your password reset code is: {HtmlEncoder.Default.Encode(resetCode)}");

    private async Task SendAsync(string email, string subject, string body)
    {
        using var client = new SmtpClient(configuration["Email:SmtpHost"], configuration.GetValue("Email:SmtpPort", 587))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(configuration["Email:SmtpUsername"], configuration["Email:SmtpPassword"])
        };
        using var message = new MailMessage
        {
            From = new MailAddress(configuration["Email:From"]!, "Work Wise"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(email);
        await client.SendMailAsync(message);
    }
}
