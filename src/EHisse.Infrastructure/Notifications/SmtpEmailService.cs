using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EHisse.Application.Interfaces;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace EHisse.Infrastructure.Notifications
{
    public class SmtpEmailService : INotificationService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly IConfiguration _config;

        public SmtpEmailService(ILogger<SmtpEmailService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public Task SendSmsAsync(string? phone, string message)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Task.CompletedTask;
            _logger.LogInformation("[SMS -> {Phone}] {Message}", phone, message);
            return Task.CompletedTask;
        }

        public async Task SendEmailAsync(string? email, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(email)) return;

            try
            {
                var server = _config["SmtpSettings:Server"] ?? "smtp.gmail.com";
                var portStr = _config["SmtpSettings:Port"] ?? "587";
                int.TryParse(portStr, out int port);
                if (port == 0) port = 587;

                var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "ehisse.az@gmail.com";
                var senderName = _config["SmtpSettings:SenderName"] ?? "E-Hisse.az";
                var password = _config["SmtpSettings:Password"]?.Replace(" ", "") ?? "ksvzyqmnwcavdnts";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(email.Trim()));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                // Linux / Cloud hosting mühitlərində sertifikat xətalarını aşmaq üçün
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Port 587 üçün StartTls ən etibarlı rejimdir
                await client.ConnectAsync(server, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("MailKit: Email uğurla göndərildi: {Email} | Mövzu: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailKit: Email göndərilərkən xəta: {Email}", email);
            }
        }

        public async Task NotifyAsync(string? phone, string? email, string subject, string message)
        {
            await SendSmsAsync(phone, message);
            await SendEmailAsync(email, subject, message);
        }
    }
}
