using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace OpenLearning.Storage.Services;

/// <summary>
/// Aliyun OSS blob provider implemented against the OSS REST API (async,
/// net8-native; no third-party SDK). Endpoint is the region endpoint WITHOUT
/// the bucket (e.g. <c>oss-cn-hangzhou.aliyuncs.com</c>); objects use
/// virtual-hosted style <c>https://{bucket}.{endpoint}/{key}</c> and
/// CanonicalizedResource <c>/{bucket}/{key}</c> for signing.
/// </summary>
public sealed class AliyunOssProvider : IStorageProvider
{
    private readonly HttpClient _http;
    private readonly string _bucket;
    private readonly string _endpoint;
    private readonly string _accessKeyId;
    private readonly string _secretAccessKey;

    public AliyunOssProvider(AliyunOssProviderOptions options, HttpClient http)
    {
        _bucket = options.Bucket;
        _endpoint = options.Endpoint.Trim().TrimEnd('/');
        _accessKeyId = options.AccessKeyId;
        _secretAccessKey = options.SecretAccessKey;
        _http = http;
    }

    public async Task SaveAsync(Stream stream, string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, BuildUrl(key))
        {
            Content = new StreamContent(stream),
        };
        Sign(request, key);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(key));
        Sign(request, key);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, BuildUrl(key));
        Sign(request, key);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NoContent &&
            response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private Uri BuildUrl(string key)
    {
        var baseUrl = $"https://{_bucket}.{_endpoint}/";
        return new Uri(baseUrl + key);
    }

    private void Sign(HttpRequestMessage request, string key)
    {
        var date = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
        request.Headers.Date = DateTimeOffset.UtcNow;
        request.Headers.TryAddWithoutValidation("x-oss-date", date);
        var contentType = request.Content?.Headers.ContentType?.ToString() ?? string.Empty;
        var canonicalizedResource = $"/{_bucket}/{key}";
        var stringToSign = $"{request.Method}\n\n{contentType}\n{date}\n{canonicalizedResource}";
        var signature = Convert.ToBase64String(HmacSha1(_secretAccessKey, stringToSign));
        request.Headers.Authorization = new AuthenticationHeaderValue("OSS", $"{_accessKeyId}:{signature}");
    }

    // Aliyun OSS mandates HMAC-SHA1 for request signing; the algorithm is a
    // fixed protocol requirement, not a choice.
#pragma warning disable CA5350, S4790
    private static byte[] HmacSha1(string key, string data)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
#pragma warning restore CA5350, S4790
}
