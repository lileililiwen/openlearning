using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace OpenLearning.Payments.Services;

public sealed class PaymentOptions
{
    public string Provider { get; set; } = "sandbox";
    public string WebhookSecret { get; set; } = "";
}

public sealed record ProviderSession(string IntentId, string RedirectUrl);
public sealed record VerifiedPaymentEvent(string EventId, string IntentId, string Type, decimal Amount, string Currency);
public interface IPaymentProvider
{
    string Name { get; }
    Task<ProviderSession> CreateAsync(Guid localId, decimal amount, string currency, CancellationToken ct = default);
    bool TryVerify(ReadOnlySpan<byte> body, string signature, out VerifiedPaymentEvent? paymentEvent);
    Task<string> RefundAsync(string providerIntentId, decimal amount, CancellationToken ct = default);
    Task<string> GetStateAsync(string providerIntentId, CancellationToken ct = default);
}

public sealed class SandboxPaymentProvider : IPaymentProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IOptions<PaymentOptions> _options;

    public SandboxPaymentProvider(IOptions<PaymentOptions> options)
    {
        _options = options;
    }

    public string Name => "sandbox";
    public Task<ProviderSession> CreateAsync(Guid localId, decimal amount, string currency, CancellationToken ct = default)
    {
        return Task.FromResult(new ProviderSession($"sb_{localId:N}", $"/Payments/Sandbox?id={localId}"));
    }

    public bool TryVerify(ReadOnlySpan<byte> body, string signature, out VerifiedPaymentEvent? paymentEvent)
    {
        paymentEvent = null;
        var secret = _options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return false;
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.ToLowerInvariant())))
            return false;
        try
        {
            paymentEvent = JsonSerializer.Deserialize<VerifiedPaymentEvent>(body, _jsonOptions);
            return paymentEvent is not null;
        }
        catch (JsonException) { return false; }
    }

    public Task<string> RefundAsync(string providerIntentId, decimal amount, CancellationToken ct = default)
    {
        return Task.FromResult($"rf_{Guid.NewGuid():N}");
    }

    public Task<string> GetStateAsync(string providerIntentId, CancellationToken ct = default)
    {
        return Task.FromResult("pending");
    }
}

public sealed class DeterministicFakePaymentProvider(string secret = "test-secret") : IPaymentProvider
{
    private readonly PaymentOptions _options = new() { WebhookSecret = secret };
    public string Name => "fake";
    public Task<ProviderSession> CreateAsync(Guid localId, decimal amount, string currency, CancellationToken ct = default)
    {
        return Task.FromResult(new ProviderSession($"fake_{localId:N}", $"/Payments/Sandbox?id={localId}"));
    }

    public bool TryVerify(ReadOnlySpan<byte> body, string signature, out VerifiedPaymentEvent? paymentEvent)
    {
        return new SandboxPaymentProvider(Options.Create(_options)).TryVerify(body, signature, out paymentEvent);
    }

    public Task<string> RefundAsync(string providerIntentId, decimal amount, CancellationToken ct = default)
    {
        return Task.FromResult($"refund_{providerIntentId}_{amount:0.00}");
    }

    public Task<string> GetStateAsync(string providerIntentId, CancellationToken ct = default)
    {
        return Task.FromResult("pending");
    }
}
