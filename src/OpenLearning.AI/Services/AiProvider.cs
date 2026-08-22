namespace OpenLearning.AI.Services;

public sealed record AiProviderRequest(string SystemInstruction, string UserText, IReadOnlyList<AiGroundingChunk> Sources);
public sealed record AiGroundingChunk(int SourceId, string Title, string Anchor, string Content);
public sealed record AiProviderResponse(string Text, int InputTokens, int OutputTokens, int? SuggestedScore = null, string? RubricEvidence = null);

public interface IAiProvider
{
    string Name { get; }
    Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, string model, CancellationToken cancellationToken);
}

public sealed class SandboxAiProvider : IAiProvider
{
    public string Name => "sandbox";

    public Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, string model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = request.Sources.Count == 0 ? null : request.Sources[0];
        var text = source is null
            ? "The approved course sources are insufficient. Please ask your instructor."
            : $"Based on {source.Title}: {source.Content[..Math.Min(source.Content.Length, 240)]}";
        int? score = request.SystemInstruction.Contains("rubric", StringComparison.OrdinalIgnoreCase) ? 75 : null;
        return Task.FromResult(new AiProviderResponse(text, Estimate(request.UserText), Estimate(text), score, score is null ? null : "Sandbox rubric evidence; human review required."));
    }

    private static int Estimate(string value)
    {
        return Math.Max(1, value.Length / 4);
    }
}
