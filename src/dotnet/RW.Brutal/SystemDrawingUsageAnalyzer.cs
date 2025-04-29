using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace RW.Brutal;

/// <summary>
///     Registers this class as an analyzer that inspects IReferenceExpression nodes (e.g., references to types or
///     members) to adda  custom warning to flag unwanted code usage, in this case for "System.Drawing".
/// </summary>
[ElementProblemAnalyzer(typeof(IReferenceExpression))]
public class SystemDrawingUsageAnalyzer : ElementProblemAnalyzer<IReferenceExpression>
{
    // Called for each IReferenceExpression in the code
    protected override void Run(IReferenceExpression referenceExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
        // Try to resolve the referenced element (eg. a class like Color)
        var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;
        
        // Only proceed if the resolved element is a type (class, struct, etc.)
        if (declaredElement is not ITypeElement typeElement) return;
        
        // Get the fully qualified namespace name for the type (eg. "System.Drawing")
        var namespaceName = typeElement.GetContainingNamespace().QualifiedName;
        
        // If the namespace starts with "System.Drawing", register a warning highlighting
        if (namespaceName != null && namespaceName.StartsWith("System.Drawing", StringComparison.Ordinal))
            consumer.AddHighlighting(new SystemDrawingUsageWarning(referenceExpression));
    }
}
