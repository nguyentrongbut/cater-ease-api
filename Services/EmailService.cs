using System.Net;
using System.Net.Mail;
namespace cater_ease_api.Dtos.Service;

public class EmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpClient = new SmtpClient(Environment.GetEnvironmentVariable("SMTP_HOST"))
        {
            Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")!),
            Credentials = new NetworkCredential(
                Environment.GetEnvironmentVariable("SMTP_USERNAME"),
                Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(Environment.GetEnvironmentVariable("SMTP_FROM")),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(to);
        await smtpClient.SendMailAsync(mailMessage);
    }
}
