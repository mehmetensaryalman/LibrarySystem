namespace LibrarySystem.Application.Interfaces.Email;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetUrl);
}