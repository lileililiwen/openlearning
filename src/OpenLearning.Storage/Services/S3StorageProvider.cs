using Amazon.S3;
using Amazon.S3.Model;

namespace OpenLearning.Storage.Services;

/// <summary>
/// S3-compatible blob provider (AWS S3, MinIO, or any S3 endpoint). Keys are
/// the platform's server-generated <c>{purpose}/{guid}{ext}</c> paths, used
/// verbatim as object keys.
/// </summary>
public sealed class S3StorageProvider : IStorageProvider, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;

    public S3StorageProvider(S3ProviderOptions options)
    {
        _bucket = options.Bucket;
        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = options.PathStyle,
            AuthenticationRegion = options.Region,
        };
        _client = new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, config);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task SaveAsync(Stream stream, string key, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            AutoCloseStream = false,
        };
        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(
                new GetObjectRequest { BucketName = _bucket, Key = key }, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        // DeleteObject is idempotent for S3; rendition objects are pruned by
        // the caller through the same provider.
        await _client.DeleteObjectAsync(_bucket, key, cancellationToken);
    }
}
