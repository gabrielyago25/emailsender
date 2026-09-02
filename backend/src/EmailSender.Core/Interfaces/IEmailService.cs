using EmailSender.Core.Models;

namespace EmailSender.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default);
}