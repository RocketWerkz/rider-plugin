using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace RW.Brutal;

/// <summary>
///     Registers this class as an analyzer that inspects IReferenceExpression nodes (e.g., references to types or
///     members) to add a custom warning to flag unwanted code usage, in this case for "System.Drawing" and
///     "Console.WriteLine".
/// </summary>
[ElementProblemAnalyzer(typeof(IReferenceExpression))]
public class UsageAnalyzer : ElementProblemAnalyzer<IReferenceExpression>
{
    // Called for each IReferenceExpression in the code
    protected override void Run(IReferenceExpression referenceExpression, ElementProblemAnalyzerData data,
        IHighlightingConsumer consumer)
    {
        var declaredElement = referenceExpression.Reference.Resolve().DeclaredElement;

        // Check if a type reference (System.Drawing)
        if (declaredElement is ITypeElement typeElement)
        {
            var namespaceName = typeElement.GetContainingNamespace().QualifiedName;
            if (namespaceName != null && namespaceName.StartsWith("System.Drawing", StringComparison.Ordinal))
            {
                consumer.AddHighlighting(new SystemDrawingUsageWarning(referenceExpression));
                return;
            }
        }
        
        // Check if a method reference (Console.WriteLine)
        if (declaredElement is IMethod method)
        {
            var containingType = method.ContainingType;
            if (containingType == null) return;
            var fullTypeName = containingType.GetClrName().FullName;
            if (fullTypeName == "System.Console" && method.ShortName == "WriteLine")
                consumer.AddHighlighting(new ConsoleWriteLineUsageWarning(referenceExpression));
        }
    }
}
