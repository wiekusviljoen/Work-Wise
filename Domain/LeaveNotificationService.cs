using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using LeaveWise.Data;

namespace LeaveWise.Domain;

public sealed class LeaveNotificationService(IConfiguration configuration, ILogger<LeaveNotificationService> logger)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"])
        && !string.IsNullOrWhiteSpace(configuration["Email:From"]);

    public async Task<bool> NotifyOwnersAsync(ApplicationDbContext db, LeaveRequestRecord request, string employeeName, string organizationName)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Leave request {RequestId} saved but email is not configured", request.Id);
            return false;
        }

        var addresses = await (from member in db.OrganizationMemberships
            join user in db.Users on member.UserId equals user.Id
            where member.OrganizationId == request.EmploymentRecord.OrganizationId
                && member.IsActive && member.Role == "Owner" && user.EmailConfirmed
            select user.Email).Distinct().ToListAsync();
        addresses = addresses.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (addresses.Count == 0)
        {
            logger.LogWarning("No verified owner email address for leave request {RequestId}", request.Id);
            return false;
        }

        var host = configuration["Email:SmtpHost"]!;
        var from = configuration["Email:From"]!;
        var port = configuration.GetValue("Email:SmtpPort", 587);
        var username = configuration["Email:SmtpUsername"];
        var password = configuration["Email:SmtpPassword"];
        using var client = new SmtpClient(host, port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);
        using var message = new MailMessage { From = new MailAddress(from), Subject = "Leave request awaiting review - Work-Wise", Body = $"A leave request is awaiting review.\n\nCompany: {organizationName}\nEmployee: {employeeName}\nType: {request.LeaveType}\nDates: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}\n\nSign in to Work-Wise and open Leave approvals to approve or decline.\n\nFor privacy and security, decisions must be made inside Work-Wise, not by replying to this email." };
        foreach (var address in addresses)
            message.To.Add(address!);
        try
        {
            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogError(ex, "Could not email employer for leave request {RequestId}", request.Id);
            return false;
        }
    }
}
