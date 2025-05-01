using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace RW.Brutal;

/// <summary>
///     Marks any usage of "Console.WriteLine" when triggered by `UsageAnalyzer` and displays a message in the status
///     bar and editor margin as a non-intrusive warning.
///     Registers this class as a WARNING-level highlight in the Rider editor.
///     Appears in the Identifier Highlightings group (used for naming-related or symbol-based highlights).
///     "OverlapResolveKind.NONE" allows other highlights to coexist on the same element.
///     "ShowToolTipInStatusBar=true" means the tooltip will be shown in Rider's status bar when the cursor hovers over
///     the item.
/// </summary>
[StaticSeverityHighlighting(
    Severity.WARNING,
    typeof(HighlightingGroupIds.IdentifierHighlightings),
    OverlapResolve = OverlapResolveKind.NONE,
    ShowToolTipInStatusBar = true)]
public class ConsoleWriteLineUsageWarning(IReferenceExpression referenceExpression) : IHighlighting
{
    // Determines whether this highlight is still valid (eg. the reference hasn't been removed or invalidated)
    public bool IsValid() => referenceExpression.IsValid();
    
    // Returns the location (text range) in the document to which this highlight applies
    public DocumentRange CalculateRange() => referenceExpression.GetDocumentRange();

    // The tooltip message shown when the user hovers over the highlighted element
    public string ToolTip => "Usage of Console.WriteLine is not recommended.";
    
    // The message shown in the error stripe when this highlight is present
    public string ErrorStripeToolTip => ToolTip;
}
