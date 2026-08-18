using System.Net;
using System.Net.Mail;

namespace OpenLearning.Notifications.Email;

#pragma warning disable CS0618 // SmtpClient is obsolete but remains the stdlib SMTP option.

/// <summary>
/// SMTP-backed sender enabled when Email:Enabled is true. Uses the classic
/// System.Net.Mail client; best-effort delivery that never blocks in-app
/// notifications.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _from;
    private readonly string? _user;
    private readonly string? _password;
    private readonly bool _useSsl;

    public SmtpEmailSender(string host, int port, string from, string? user, string? password, bool useSsl)
    {
        _host = host;
        _port = port;
        _from = from;
        _user = user;
        _password = password;
        _useSsl = useSsl;
    }

    public async Task SendAsync(string toAddress, string subject, string body)
    {
        // S5332: SSL support depends on the configured SMTP server (local dev servers
        // often run without TLS); the flag is driven by the Email:UseSsl config value.
#pragma warning disable S5332
        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _useSsl,
        };
#pragma warning restore S5332
        if (!string.IsNullOrWhiteSpace(_user))
        {
            client.Credentials = new NetworkCredential(_user, _password);
        }

        var message = new MailMessage(_from, toAddress, subject, body);
        try
        {
            await client.SendMailAsync(message);
        }
        finally
        {
            message.Dispose();
        }
    }
}

#pragma warning restore CS0618
