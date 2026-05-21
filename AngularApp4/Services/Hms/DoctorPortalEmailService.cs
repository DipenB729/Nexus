using System.Net;
using System.Net.Mail;
using AngularApp4.Data;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Services.Hms;

public interface IDoctorPortalEmailService
{
    Task<DoctorPortalEmailResult> SendCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string loginEmail,
        string temporaryPassword,
        string portalUrl,
        CancellationToken cancellationToken = default);

    Task<DoctorPortalEmailResult> SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetCode,
        DateTime expiresAtUtc,
        string portalUrl,
        CancellationToken cancellationToken = default);
}

public sealed class DoctorPortalEmailResult
{
    public bool Sent { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class DoctorPortalEmailService : IDoctorPortalEmailService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DoctorPortalEmailService> _logger;

    public DoctorPortalEmailService(AppDbContext db, ILogger<DoctorPortalEmailService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DoctorPortalEmailResult> SendCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string loginEmail,
        string temporaryPassword,
        string portalUrl,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.SystemControlSettings
            .AsNoTracking()
            .OrderBy(x => x.SystemControlSettingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return new DoctorPortalEmailResult
            {
                Message = "Portal account was created, but system email settings are missing."
            };
        }

        if (string.IsNullOrWhiteSpace(settings.EmailApiUrl) ||
            string.IsNullOrWhiteSpace(settings.EmailFromAddress) ||
            string.IsNullOrWhiteSpace(settings.EmailApiKey))
        {
            return new DoctorPortalEmailResult
            {
                Message = "Portal account was created, but email sender settings are incomplete."
            };
        }

        var host = settings.EmailApiUrl.Trim();
        var port = settings.EmailSmtpPort > 0 ? settings.EmailSmtpPort : 587;

        var provider = settings.EmailProviderName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (provider.Length > 0 && provider is not ("gmail" or "smtp"))
        {
            return new DoctorPortalEmailResult
            {
                Message = $"Portal account was created, but provider '{settings.EmailProviderName}' is not configured for doctor credential emails."
            };
        }

        var body =
            $"Hello {recipientName},{Environment.NewLine}{Environment.NewLine}" +
            "Your doctor portal account has been created by the hospital administrator." + Environment.NewLine + Environment.NewLine +
            $"Portal URL: {portalUrl}{Environment.NewLine}" +
            $"Login ID: {loginEmail}{Environment.NewLine}" +
            $"Temporary Password: {temporaryPassword}{Environment.NewLine}{Environment.NewLine}" +
            "Please sign in and change your password immediately." + Environment.NewLine + Environment.NewLine +
            "Regards," + Environment.NewLine +
            "Nexus Hospital";

        using var message = new MailMessage(settings.EmailFromAddress, recipientEmail)
        {
            Subject = "Your doctor portal login credentials",
            Body = body,
            IsBodyHtml = false
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = settings.EmailUseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(
                string.IsNullOrWhiteSpace(settings.EmailSmtpUsername) ? settings.EmailFromAddress : settings.EmailSmtpUsername,
                settings.EmailApiKey)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return new DoctorPortalEmailResult
            {
                Sent = true,
                Message = $"Portal credentials were emailed to {recipientEmail}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send doctor portal credentials to {RecipientEmail}", recipientEmail);
            return new DoctorPortalEmailResult
            {
                Message = $"Portal account was created, but email delivery failed: {ex.Message}"
            };
        }
    }

    public async Task<DoctorPortalEmailResult> SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetCode,
        DateTime expiresAtUtc,
        string portalUrl,
        CancellationToken cancellationToken = default)
    {
        var body =
            $"Hello {recipientName},{Environment.NewLine}{Environment.NewLine}" +
            "A password reset code was generated for your Nexus admin account." + Environment.NewLine + Environment.NewLine +
            $"Portal URL: {portalUrl}{Environment.NewLine}" +
            $"Email: {recipientEmail}{Environment.NewLine}" +
            $"Reset Code: {resetCode}{Environment.NewLine}" +
            $"Expires UTC: {expiresAtUtc:yyyy-MM-dd HH:mm}{Environment.NewLine}{Environment.NewLine}" +
            "If you did not request this, contact your superadmin." + Environment.NewLine + Environment.NewLine +
            "Regards," + Environment.NewLine +
            "Nexus Hospital";

        return await SendMailAsync(
            recipientEmail,
            "Nexus admin password reset code",
            body,
            "Password reset email could not be sent because email sender settings are missing or incomplete.",
            cancellationToken);
    }

    private async Task<DoctorPortalEmailResult> SendMailAsync(
        string recipientEmail,
        string subject,
        string body,
        string settingsErrorMessage,
        CancellationToken cancellationToken)
    {
        var settings = await _db.SystemControlSettings
            .AsNoTracking()
            .OrderBy(x => x.SystemControlSettingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.EmailApiUrl) ||
            string.IsNullOrWhiteSpace(settings.EmailFromAddress) ||
            string.IsNullOrWhiteSpace(settings.EmailApiKey))
        {
            return new DoctorPortalEmailResult { Message = settingsErrorMessage };
        }

        var host = settings.EmailApiUrl.Trim();
        var port = settings.EmailSmtpPort > 0 ? settings.EmailSmtpPort : 587;
        var provider = settings.EmailProviderName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (provider.Length > 0 && provider is not ("gmail" or "smtp"))
        {
            return new DoctorPortalEmailResult
            {
                Message = $"Email provider '{settings.EmailProviderName}' is not configured for SMTP delivery."
            };
        }

        using var message = new MailMessage(settings.EmailFromAddress, recipientEmail)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = settings.EmailUseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(
                string.IsNullOrWhiteSpace(settings.EmailSmtpUsername) ? settings.EmailFromAddress : settings.EmailSmtpUsername,
                settings.EmailApiKey)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return new DoctorPortalEmailResult
            {
                Sent = true,
                Message = $"Email sent to {recipientEmail}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to send email to {RecipientEmail}", recipientEmail);
            return new DoctorPortalEmailResult
            {
                Message = $"Email delivery failed: {ex.Message}"
            };
        }
    }

}
