namespace CoachingPlatform.Application.Common;

public interface IMessageTransport
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    Task SendSmsAsync(string to, string body, CancellationToken ct = default);
}
