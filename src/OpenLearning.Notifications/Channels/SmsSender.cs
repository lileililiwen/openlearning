using Microsoft.Extensions.Logging;

namespace OpenLearning.Notifications.Channels;

/// <summary>
/// Provider-adapter point for SMS. The reference system has no concrete SMS
/// gateway, so this adapter logs the message (observable in dev) and is the
/// seam a real provider would replace. Only registered when
/// Messaging:SmsEnabled is true.
/// </summary>
public sealed class SmsSender : ISmsSender
{
    private static readonly Action<ILogger, string, string, Exception?> _logSms =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "SmsSent"),
            "SMS to {Phone}: {Message}");

    private readonly ILogger<SmsSender> _logger;

    public SmsSender(ILogger<SmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message)
    {
        _logSms(_logger, phoneNumber, message, null);
        return Task.CompletedTask;
    }
}
