using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace RW.Brutal;

/// <summary>
///     Registers this class as an analyzer that inspects IObjectCreationExpression nodes to add a custom warning to
///     flag unwanted code usage, in this case for `new Vector2()` or similar constructor calls.
/// </summary>
[ElementProblemAnalyzer(typeof(IObjectCreationExpression))]
public class ObjectAnalyzer : ElementProblemAnalyzer<IObjectCreationExpression>
{
    // Called for each IObjectCreationExpression in the code
    protected override void Run(IObjectCreationExpression creationExpression, ElementProblemAnalyzerData data,
        IHighlightingConsumer consumer)
    {
        var type = creationExpression.Type() as IDeclaredType;
        var resolved = type?.GetTypeElement();

        var namespaceName = resolved?.GetContainingNamespace().QualifiedName;
        if (namespaceName is not null && namespaceName.StartsWith("System.Numerics", StringComparison.Ordinal))
            consumer.AddHighlighting(new SystemNumericsUsageWarning(creationExpression));
    }
}
