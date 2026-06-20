using System.Net;
using System.Net.Mail;

namespace Employee_Department.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "ระบบจัดการพนักงาน";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;   // Gmail App Password (16 digits)
        public bool EnableSsl { get; set; } = true;
        // Override recipient (for testing). If set, ALL emails go here.
        public string? OverrideRecipient { get; set; }
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _settings = new EmailSettings();
            config.GetSection("EmailSettings").Bind(_settings);
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var recipient = string.IsNullOrWhiteSpace(_settings.OverrideRecipient)
                ? toEmail
                : _settings.OverrideRecipient;

            try
            {
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using var msg = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };
                msg.To.Add(recipient);

                await client.SendMailAsync(msg);
                _logger.LogInformation("Email sent to {Recipient}: {Subject}", recipient, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
                // Don't crash the app - fail silently but log. Caller can check log.
                throw;
            }
        }
    }
}
