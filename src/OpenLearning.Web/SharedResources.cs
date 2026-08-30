using System.Diagnostics.CodeAnalysis;

namespace OpenLearning.Web;

/// <summary>
/// Marker type that groups shared UI-chrome resource strings so they can be resolved
/// via <c>IStringLocalizer&lt;SharedResources&gt;</c> from any view. The class carries no
/// members; the strings live in <c>Resources/SharedResources.zh.resx</c> and the English
/// text is used as the key (so an untranslated string falls back to English).
/// </summary>
[SuppressMessage("SonarAnalyzer", "S2094", Justification = "Marker type used only to scope shared UI-chrome resources; it intentionally has no members.")]
public class SharedResources
{
}
