using System.Net;
using System.Net.Mail;
using AtiqSalon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class NotificationDispatcher(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<NotificationDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try { await Dispatch(stoppingToken); }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Notification dispatch cycle failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Dispatch(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messages = await db.NotificationMessages.IgnoreQueryFilters()
            .Where(x => (x.Status == "Pending" || x.Status == "Scheduled")
                && (x.ScheduledForUtc == null || x.ScheduledForUtc <= DateTimeOffset.UtcNow))
            .OrderBy(x => x.ScheduledForUtc).Take(25).ToListAsync(ct);
        foreach (var message in messages)
        {
            try
            {
                using var smtp = new SmtpClient(configuration["SMTP_HOST"] ?? "localhost", configuration.GetValue("SMTP_PORT", 1025))
                {
                    EnableSsl = configuration.GetValue("SMTP_USE_TLS", false)
                };
                var username = configuration["SMTP_USERNAME"];
                if (!string.IsNullOrWhiteSpace(username))
                    smtp.Credentials = new NetworkCredential(username, configuration["SMTP_PASSWORD"]);
                using var mail = new MailMessage(configuration["SMTP_FROM"] ?? "bookings@atiqsalon.local", message.Recipient, message.Subject ?? "AtiqSalon notification", message.Body);
                await smtp.SendMailAsync(mail, ct);
                message.Status = "Sent";
                message.SentAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception exception)
            {
                message.Status = "Failed";
                message.FailedAtUtc = DateTimeOffset.UtcNow;
                message.FailureReason = exception.GetType().Name;
            }
        }
        if (messages.Count > 0) await db.SaveChangesAsync(ct);
    }
}
