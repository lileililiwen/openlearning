using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Storage.Services;

/// <summary>
/// Builds the active <see cref="IStorageProvider"/> from the configured
/// strategy. The strategy and provider options come from system-config
/// (admin-editable); appsettings keys (<c>Storage:Provider</c>,
/// <c>Storage:S3:*</c>, <c>Storage:Oss:*</c>) win when present so operators can
/// pin a strategy in config. Applied on the next application start.
/// </summary>
public static class StorageProviderFactory
{
    public static async Task<IStorageProvider> CreateAsync(
        IConfiguration configuration, SystemConfigService config, string localRoot, IHttpClientFactory httpClientFactory)
    {
        var provider = configuration["Storage:Provider"]
            ?? await config.GetStringAsync("Storage.Provider", "Local");
        switch (provider.Trim().ToLowerInvariant())
        {
            case "s3":
                return new S3StorageProvider(new S3ProviderOptions(
                    configuration["Storage:S3:Endpoint"] ?? await config.GetStringAsync("Storage.S3.Endpoint", string.Empty),
                    configuration["Storage:S3:Bucket"] ?? await config.GetStringAsync("Storage.S3.Bucket", string.Empty),
                    configuration["Storage:S3:AccessKeyId"] ?? await config.GetStringAsync("Storage.S3.AccessKeyId", string.Empty),
                    configuration["Storage:S3:SecretAccessKey"] ?? await config.GetStringAsync("Storage.S3.SecretAccessKey", string.Empty),
                    configuration["Storage:S3:Region"] ?? await config.GetStringAsync("Storage.S3.Region", "us-east-1"),
                    configuration.GetValue("Storage:S3:PathStyle", false)));

            // MinIO is a self-hosted S3-compatible backend; path-style
            // addressing is always required for it.
            case "minio":
                return new S3StorageProvider(new S3ProviderOptions(
                    configuration["Storage:S3:Endpoint"] ?? await config.GetStringAsync("Storage.S3.Endpoint", "http://localhost:9000"),
                    configuration["Storage:S3:Bucket"] ?? await config.GetStringAsync("Storage.S3.Bucket", string.Empty),
                    configuration["Storage:S3:AccessKeyId"] ?? await config.GetStringAsync("Storage.S3.AccessKeyId", string.Empty),
                    configuration["Storage:S3:SecretAccessKey"] ?? await config.GetStringAsync("Storage.S3.SecretAccessKey", string.Empty),
                    configuration["Storage:S3:Region"] ?? await config.GetStringAsync("Storage.S3.Region", "us-east-1"),
                    PathStyle: true));

            case "aliyunoss":
            case "oss":
                return new AliyunOssProvider(
                    new AliyunOssProviderOptions(
                        configuration["Storage:Oss:Endpoint"] ?? await config.GetStringAsync("Storage.Oss.Endpoint", string.Empty),
                        configuration["Storage:Oss:Bucket"] ?? await config.GetStringAsync("Storage.Oss.Bucket", string.Empty),
                        configuration["Storage:Oss:AccessKeyId"] ?? await config.GetStringAsync("Storage.Oss.AccessKeyId", string.Empty),
                        configuration["Storage:Oss:SecretAccessKey"] ?? await config.GetStringAsync("Storage.Oss.SecretAccessKey", string.Empty)),
                    httpClientFactory.CreateClient("OpenLearning.Storage.Oss"));

            default:
                return new LocalStorageProvider(localRoot);
        }
    }
}
