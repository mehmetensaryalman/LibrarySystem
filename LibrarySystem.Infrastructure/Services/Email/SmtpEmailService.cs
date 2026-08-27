using System.Net;
using System.Net.Mail;
using System.Text;
using LibrarySystem.Application.Interfaces.Email;
using Microsoft.Extensions.Configuration;

namespace LibrarySystem.Infrastructure.Services.Email;

public class SmtpEmailService :
    IEmailService
{
    private readonly IConfiguration
        _configuration;

    public SmtpEmailService(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }

    public async Task
        SendPasswordResetEmailAsync(
            string recipientEmail,
            string resetUrl)
    {
        var host =
            GetRequiredConfiguration(
                "Smtp:Host");

        var port =
            _configuration.GetValue<int>(
                "Smtp:Port");

        if (port <= 0)
        {
            throw new InvalidOperationException(
                "Smtp:Port configuration is missing or invalid.");
        }

        var enableSsl =
            _configuration.GetValue(
                "Smtp:EnableSsl",
                true);

        var userName =
            GetRequiredConfiguration(
                "Smtp:UserName");

        var password =
            GetRequiredConfiguration(
                "Smtp:Password");

        var fromEmail =
            GetRequiredConfiguration(
                "Smtp:FromEmail");

        var fromName =
            _configuration[
                "Smtp:FromName"]
            ?? "Kütüphane Sistemi";

        var safeResetUrl =
            WebUtility.HtmlEncode(
                resetUrl);

        var emailBody =
            $"""
             <!DOCTYPE html>
             <html lang="tr">
             <head>
                 <meta charset="UTF-8">
                 <meta name="viewport" content="width=device-width, initial-scale=1.0">
                 <title>Parola Sıfırlama</title>
             </head>
             <body style="margin:0;padding:0;background:#f5f7fa;font-family:Arial,Helvetica,sans-serif;color:#334155;">
                 <div style="max-width:600px;margin:0 auto;padding:32px 16px;">
                     <div style="background:#ffffff;border:1px solid #e2e8f0;border-radius:16px;padding:32px;box-shadow:0 12px 30px rgba(15,23,42,0.08);">
                         <h1 style="margin:0;color:#0f172a;font-size:26px;text-align:center;">
                             Kütüphane Sistemi
                         </h1>

                         <h2 style="margin:24px 0 12px;color:#334155;font-size:20px;">
                             Parolanızı sıfırlayın
                         </h2>

                         <p style="margin:0 0 20px;color:#64748b;font-size:15px;line-height:1.6;">
                             Hesabınız için bir parola sıfırlama isteği aldık.
                             Yeni parolanızı belirlemek için aşağıdaki düğmeye tıklayın.
                         </p>

                         <div style="text-align:center;margin:28px 0;">
                             <a
                                 href="{safeResetUrl}"
                                 style="display:inline-block;padding:13px 22px;border-radius:9px;background:#0f9f8f;color:#ffffff;text-decoration:none;font-size:15px;font-weight:700;"
                             >
                                 Parolamı Sıfırla
                             </a>
                         </div>

                         <p style="margin:0 0 10px;color:#64748b;font-size:14px;line-height:1.6;">
                             Bu bağlantı 30 dakika boyunca geçerlidir ve yalnızca bir kez kullanılabilir.
                         </p>

                         <p style="margin:0;color:#64748b;font-size:14px;line-height:1.6;">
                             Bu işlemi siz istemediyseniz e-postayı dikkate almayabilirsiniz.
                         </p>
                     </div>
                 </div>
             </body>
             </html>
             """;

        using var message =
            new MailMessage
            {
                From =
                    new MailAddress(
                        fromEmail,
                        fromName),

                Subject =
                    "Kütüphane Sistemi - Parola Sıfırlama",

                Body =
                    emailBody,

                IsBodyHtml =
                    true,

                BodyEncoding =
                    Encoding.UTF8,

                SubjectEncoding =
                    Encoding.UTF8
            };

        message.To.Add(
            recipientEmail);

        using var smtpClient =
            new SmtpClient(
                host,
                port)
            {
                EnableSsl =
                    enableSsl,

                UseDefaultCredentials =
                    false,

                Credentials =
                    new NetworkCredential(
                        userName,
                        password),

                DeliveryMethod =
                    SmtpDeliveryMethod.Network
            };

        await smtpClient
            .SendMailAsync(
                message);
    }

    private string
        GetRequiredConfiguration(
            string key)
    {
        var value =
            _configuration[key];

        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            throw new InvalidOperationException(
                $"{key} configuration is missing.");
        }

        return value;
    }
}