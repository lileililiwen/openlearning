using System.Globalization;
using System.Text;
using System.Text.Json;

namespace OpenLearning.Quality;

/// <summary>One CI run's quality snapshot.</summary>
public sealed record QualityRun(
    string Date,
    bool BuildPassed,
    int BuildWarnings,
    int BuildErrors,
    double? CoverageOverall,
    double? CoverageNewLines,
    int? Bugs,
    int? Vulnerabilities,
    int? CodeSmells,
    double? Duplication,
    int HighAdvisories,
    IReadOnlyList<GateStatus> Gates,
    IReadOnlyDictionary<string, string> Provenance);

/// <summary>One gate's outcome for the run (pass / fail / unavailable).</summary>
public sealed record GateStatus(string Name, string Status, string? Detail);

/// <summary>
/// Merges the CI-emitted metrics JSON files (build/coverage/sonar/audit) with
/// the run history and renders <c>docs/quality/README.md</c> plus a trend table.
/// Missing metric files degrade to defaults instead of failing.
/// </summary>
public static class Program
{
    private const int _defaultMaxTrend = 10;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static int Main(string[] args)
    {
        var (metricsDir, historyDir, outputPath, maxTrend) = ParseArgs(args);
        try
        {
            Directory.CreateDirectory(historyDir);

            var run = LoadRun(metricsDir);
            var history = LoadHistory(historyDir);
            history.RemoveAll(h => h.Date == run.Date);
            history.Insert(0, run);

            File.WriteAllText(
                Path.Combine(historyDir, $"{run.Date}.json"),
                JsonSerializer.Serialize(run, _jsonOptions));
            File.WriteAllText(outputPath, RenderDashboard(run, history, maxTrend));

            Console.WriteLine($"[quality] dashboard written to {outputPath}; history entries: {history.Count}");
            Console.WriteLine(Summarize(run));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[quality] failed: {ex.Message}");
            return 1;
        }
    }

    private static (string MetricsDir, string HistoryDir, string OutputPath, int MaxTrend) ParseArgs(string[] args)
    {
        string metricsDir = "metrics";
        string historyDir = "docs/quality/history";
        string outputPath = "docs/quality/README.md";
        int maxTrend = _defaultMaxTrend;
        var i = 0;
        while (i < args.Length)
        {
            var value = i + 1 < args.Length ? args[i + 1] : null;
            switch (args[i])
            {
                case "--metrics" when value is not null:
                    metricsDir = value;
                    i += 2;
                    break;
                case "--history" when value is not null:
                    historyDir = value;
                    i += 2;
                    break;
                case "--output" when value is not null:
                    outputPath = value;
                    i += 2;
                    break;
                case "--max-trend" when value is not null:
                    _ = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxTrend);
                    i += 2;
                    break;
                default:
                    i += 1;
                    break;
            }
        }

        return (metricsDir, historyDir, outputPath, maxTrend);
    }

    private static QualityRun LoadRun(string metricsDir)
    {
        var build = ReadJson(Path.Combine(metricsDir, "build.json"));
        var coverage = ReadJson(Path.Combine(metricsDir, "coverage.json"));
        var sonar = ReadJson(Path.Combine(metricsDir, "sonar.json"));
        var audit = ReadJson(Path.Combine(metricsDir, "audit.json"));
        var gatesJson = ReadJson(Path.Combine(metricsDir, "gates.json"));
        var provenanceJson = ReadJson(Path.Combine(metricsDir, "provenance.json"));

        return new QualityRun(
            Date: DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            BuildPassed: GetBool(build, "passed") ?? false,
            BuildWarnings: GetInt(build, "warnings") ?? 0,
            BuildErrors: GetInt(build, "errors") ?? 0,
            CoverageOverall: GetDouble(coverage, "overall"),
            CoverageNewLines: GetDouble(coverage, "newLines"),
            Bugs: GetInt(sonar, "bugs"),
            Vulnerabilities: GetInt(sonar, "vulnerabilities"),
            CodeSmells: GetInt(sonar, "codeSmells"),
            Duplication: GetDouble(sonar, "duplication"),
            HighAdvisories: GetInt(audit, "high") ?? 0,
            Gates: LoadGates(gatesJson),
            Provenance: LoadProvenance(provenanceJson));
    }

    private static IReadOnlyList<GateStatus> LoadGates(JsonElement? root)
    {
        if (root is null || !root.Value.TryGetProperty("gates", out var gates) || gates.ValueKind != JsonValueKind.Array)
        {
            // Fall back to the legacy four-gate view so older CI runs still
            // render a sensible dashboard. Missing = unavailable, not pass.
            return new[]
            {
                new GateStatus("build", root is null ? "unavailable" : "pass", null),
                new GateStatus("incremental-coverage", "unavailable", null),
                new GateStatus("evidence-manifest", "unavailable", null),
                new GateStatus("nuget-audit", "unavailable", null),
                new GateStatus("accessibility", "unavailable", null),
                new GateStatus("migration-drift", "unavailable", null),
            };
        }

        var list = new List<GateStatus>();
        foreach (var gate in gates.EnumerateArray())
        {
            var name = gate.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
            var status = gate.TryGetProperty("status", out var s) ? s.GetString() ?? "unknown" : "unknown";
            var detail = gate.TryGetProperty("detail", out var d) ? d.GetString() : null;
            list.Add(new GateStatus(name, status, detail));
        }
        return list;
    }

    private static Dictionary<string, string> LoadProvenance(JsonElement? root)
    {
        if (root is null)
        {
            return new Dictionary<string, string>();
        }

        var map = new Dictionary<string, string>();
        foreach (var prop in root.Value.EnumerateObject())
        {
            map[prop.Name] = prop.Value.ToString();
        }
        return map;
    }

    private static JsonElement? ReadJson(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    private static bool? GetBool(JsonElement? root, string property)
    {
        if (root is null || !root.Value.TryGetProperty(property, out var element) ||
            element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return element.GetBoolean();
    }

    private static int? GetInt(JsonElement? root, string property)
    {
        if (root is null || !root.Value.TryGetProperty(property, out var element) ||
            element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return element.GetInt32();
    }

    private static double? GetDouble(JsonElement? root, string property)
    {
        if (root is null || !root.Value.TryGetProperty(property, out var element) ||
            element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return element.GetDouble();
    }

    private static List<QualityRun> LoadHistory(string historyDir)
    {
        var runs = new List<QualityRun>();
        if (!Directory.Exists(historyDir))
        {
            return runs;
        }

        foreach (var file in Directory.EnumerateFiles(historyDir, "*.json"))
        {
            try
            {
                var run = JsonSerializer.Deserialize<QualityRun>(File.ReadAllText(file));
                if (run is not null)
                {
                    runs.Add(run);
                }
            }
            catch (JsonException)
            {
                // Tolerate a corrupt history entry; skip it.
            }
        }

        return runs.OrderByDescending(r => r.Date).ToList();
    }

    private static string RenderDashboard(QualityRun latest, List<QualityRun> history, int maxTrend)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# OpenLearning Quality Dashboard");
        sb.AppendLine();
        sb.AppendLine("Generated from CI metrics by `OpenLearning.Quality`. Missing metric sources show as `n/a`; missing gates show as `unavailable`.");
        sb.AppendLine();
        sb.AppendLine("## Latest run — " + latest.Date);
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Build | {(latest.BuildPassed ? "pass" : "fail")} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Build warnings | {latest.BuildWarnings} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Build errors | {latest.BuildErrors} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Coverage (overall) | {FormatPercent(latest.CoverageOverall)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Coverage (new lines) | {FormatPercent(latest.CoverageNewLines)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Bugs | {Format(latest.Bugs)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Vulnerabilities | {Format(latest.Vulnerabilities)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Code smells | {Format(latest.CodeSmells)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Duplicated lines | {FormatPercent(latest.Duplication)} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| High/critical advisories | {latest.HighAdvisories} |");
        sb.AppendLine();

        sb.AppendLine("## Release gates");
        sb.AppendLine();
        sb.AppendLine("| Gate | Status | Detail |");
        sb.AppendLine("|---|---|---|");
        foreach (var gate in latest.Gates)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| `{gate.Name}` | {gate.Status} | {Format(gate.Detail)} |");
        }
        sb.AppendLine();

        if (latest.Provenance.Count > 0)
        {
            sb.AppendLine("## Provenance");
            sb.AppendLine();
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("|---|---|");
            foreach (var (key, value) in latest.Provenance.OrderBy(p => p.Key))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {key} | {value} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Trend (last " + maxTrend.ToString(CultureInfo.InvariantCulture) + " runs)");
        sb.AppendLine();
        sb.AppendLine("| Date | Build | Coverage % | Bugs | Vulns | High advisories |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var run in history.Take(maxTrend))
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| {run.Date} | {(run.BuildPassed ? "pass" : "fail")} | {FormatPercent(run.CoverageOverall)} | " +
                $"{Format(run.Bugs)} | {Format(run.Vulnerabilities)} | {run.HighAdvisories} |");
        }

        return sb.ToString();
    }

    private static string Summarize(QualityRun run)
    {
        var issues = new List<string>();
        if (!run.BuildPassed)
        {
            issues.Add("build failing");
        }

        if (run.HighAdvisories > 0)
        {
            issues.Add($"{run.HighAdvisories} high/critical advisory(ies)");
        }

        if (run.Bugs is > 0)
        {
            issues.Add($"{run.Bugs} bug(s)");
        }

        if (run.Vulnerabilities is > 0)
        {
            issues.Add($"{run.Vulnerabilities} vulnerability(ies)");
        }

        foreach (var gate in run.Gates.Where(g => g.Status == "fail"))
        {
            issues.Add($"gate {gate.Name} failed");
        }

        return issues.Count == 0
            ? "[quality] no regressions detected."
            : "[quality] regressions: " + string.Join(", ", issues) + ".";
    }

    private static string Format(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "n/a";
    }

    private static string Format(string? value)
    {
        return string.IsNullOrEmpty(value) ? "n/a" : value;
    }

    private static string FormatPercent(double? value)
    {
        return value is null ? "n/a" : string.Format(CultureInfo.InvariantCulture, "{0:F1}%", value);
    }
}
