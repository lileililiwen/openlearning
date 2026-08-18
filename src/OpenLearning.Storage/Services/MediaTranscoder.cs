using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenLearning.Storage.Models;

namespace OpenLearning.Storage.Services;

/// <summary>A video awaiting rendition generation.</summary>
public sealed record TranscodeRequest(int StoredFileId, string Key);

/// <summary>
/// Background worker that turns an uploaded video into renditions. Uses FFmpeg
/// when it is available on PATH; otherwise falls back to a single-source
/// passthrough rendition so the pipeline stays runnable without it.
/// </summary>
public sealed class MediaTranscoder : BackgroundService
{
    private static readonly (string Name, int Height)[] _renditions =
    {
        ("low", 480),
        ("mid", 720),
        ("high", 1080),
    };

    private static readonly Action<ILogger, string, Exception?> _logPassthrough = LoggerMessage.Define<string>(
        LogLevel.Information, 1, "Transcoded {Key} via passthrough (ffmpeg not found).");

    private static readonly Action<ILogger, string, Exception?> _logRenditionFailed = LoggerMessage.Define<string>(
        LogLevel.Warning, 2, "FFmpeg rendition failed for {Key}.");

    private static readonly Action<ILogger, string, Exception?> _logTranscodeFailed = LoggerMessage.Define<string>(
        LogLevel.Error, 3, "Transcoding failed for {Key}.");

    private readonly Channel<TranscodeRequest> _queue = Channel.CreateUnbounded<TranscodeRequest>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStorageProvider _storage;
    private readonly ILogger<MediaTranscoder> _logger;
    private readonly string? _ffmpegPath;

    public MediaTranscoder(
        IServiceScopeFactory scopeFactory,
        IStorageProvider storage,
        ILogger<MediaTranscoder> logger)
    {
        _scopeFactory = scopeFactory;
        _storage = storage;
        _logger = logger;
        _ffmpegPath = ResolveFfmpeg();
    }

    public void Enqueue(TranscodeRequest request)
    {
        _queue.Writer.TryWrite(request);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(request, stoppingToken);
        }
    }

    private async Task ProcessAsync(TranscodeRequest request, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var asset = await db.Set<MediaAsset>()
            .FirstOrDefaultAsync(a => a.StoredFileId == request.StoredFileId, cancellationToken);
        if (asset is null)
        {
            return;
        }

        var baseKey = request.Key[..request.Key.LastIndexOf('.')];
        try
        {
            using var source = await _storage.OpenAsync(request.Key, cancellationToken);
            if (source is null)
            {
                throw new FileNotFoundException("Source blob is missing.", request.Key);
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "openlearning-transcode", request.StoredFileId.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(tempDir);
            var sourcePath = Path.Combine(tempDir, "source.mp4");
            await using (var file = File.Create(sourcePath))
            {
                await source.CopyToAsync(file, cancellationToken);
            }

            if (_ffmpegPath is null)
            {
                // Passthrough: expose the original as the single "low" rendition.
                var passthroughKey = $"{baseKey}.low.mp4";
                await using (var file = File.OpenRead(sourcePath))
                {
                    await _storage.SaveAsync(file, passthroughKey, cancellationToken);
                }

                asset.LowUrl = $"files/{passthroughKey}";
                asset.Status = RenditionStatus.Ready;
                _logPassthrough(_logger, request.Key, null);
            }
            else
            {
                var ready = true;
                foreach (var (name, height) in _renditions)
                {
                    var outPath = Path.Combine(tempDir, $"{name}.mp4");
                    var exitCode = RunFfmpeg(_ffmpegPath, sourcePath, outPath, height);
                    if (exitCode != 0)
                    {
                        ready = false;
                        break;
                    }

                    var renditionKey = $"{baseKey}.{name}.mp4";
                    await using (var file = File.OpenRead(outPath))
                    {
                        await _storage.SaveAsync(file, renditionKey, cancellationToken);
                    }

                    SetUrl(asset, name, renditionKey);
                }

                asset.Status = ready ? RenditionStatus.Ready : RenditionStatus.Failed;
                if (!ready)
                {
                    asset.Error = "FFmpeg could not produce renditions.";
                    _logRenditionFailed(_logger, request.Key, null);
                }
            }

            asset.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            asset.Status = RenditionStatus.Failed;
            asset.Error = ex.Message;
            asset.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            _logTranscodeFailed(_logger, request.Key, ex);
        }
    }

    private static void SetUrl(MediaAsset asset, string name, string key)
    {
        var url = $"files/{key}";
        switch (name)
        {
            case "low":
                asset.LowUrl = url;
                break;
            case "mid":
                asset.MidUrl = url;
                break;
            case "high":
                asset.HighUrl = url;
                break;
        }
    }

    private static int RunFfmpeg(string ffmpegPath, string sourcePath, string outputPath, int height)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(sourcePath);
        psi.ArgumentList.Add("-vf");
        psi.ArgumentList.Add($"scale=-2:{height}");
        psi.ArgumentList.Add("-c:v");
        psi.ArgumentList.Add("libx264");
        psi.ArgumentList.Add("-preset");
        psi.ArgumentList.Add("veryfast");
        psi.ArgumentList.Add("-c:a");
        psi.ArgumentList.Add("aac");
        psi.ArgumentList.Add("-b:a");
        psi.ArgumentList.Add("128k");
        psi.ArgumentList.Add(outputPath);

        using var process = Process.Start(psi);
        if (process is null)
        {
            return -1;
        }

        process.StandardError.ReadToEnd();
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>Finds ffmpeg on PATH and returns its absolute path, or null.</summary>
    private static string? ResolveFfmpeg()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        var names = OperatingSystem.IsWindows()
            ? new[] { "ffmpeg.exe", "ffmpeg" }
            : new[] { "ffmpeg" };
        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            foreach (var name in names)
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        return null;
    }
}
