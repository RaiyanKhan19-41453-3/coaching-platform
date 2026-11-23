using CoachingPlatform.Application.Common;
using System.Net.Http.Json;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace CoachingPlatform.Infrastructure.Messaging;

/// <summary>
/// Real SMTP email transport plus a configurable HTTP webhook for SMS. If neither is
/// configured, delivery is a no-op and the outbox message is still marked processed.
/// </summary>
public sealed class MessageTransport : IMessageTransport, IDisposable
{
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _from;
    private readonly string? _smsWebhook;
    private readonly HttpClient _http = new();

    public MessageTransport(IConfiguration configuration)
    {
        _smtpHost = configuration["Messaging:SmtpHost"];
        _smtpPort = int.TryParse(configuration["Messaging:SmtpPort"], out var port) ? port : 587;
        _smtpUser = configuration["Messaging:SmtpUser"];
        _smtpPassword = configuration["Messaging:SmtpPassword"];
        _from = configuration["Messaging:FromAddress"] ?? "noreply@coaching.local";
        _smsWebhook = configuration["Messaging:SmsWebhook"];
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smtpHost)) return;
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from!));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost!, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
        if (!string.IsNullOrWhiteSpace(_smtpUser))
            await client.AuthenticateAsync(_smtpUser!, _smtpPassword!, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    public async Task SendSmsAsync(string to, string body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smsWebhook)) return;
        var response = await _http.PostAsJsonAsync(_smsWebhook,
            new { to, message = body }, ct);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}
