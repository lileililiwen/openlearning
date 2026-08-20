namespace OpenLearning.Storage.Services;

/// <summary>Connection settings for an S3-compatible backend (incl. MinIO).</summary>
public sealed record S3ProviderOptions(
    string Endpoint,
    string Bucket,
    string AccessKeyId,
    string SecretAccessKey,
    string Region,
    bool PathStyle);

/// <summary>Connection settings for Aliyun OSS.</summary>
public sealed record AliyunOssProviderOptions(
    string Endpoint,
    string Bucket,
    string AccessKeyId,
    string SecretAccessKey);
