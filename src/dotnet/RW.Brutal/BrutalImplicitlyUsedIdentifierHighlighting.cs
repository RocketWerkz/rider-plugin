using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;

namespace RW.Brutal
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        AttributeId = BrutalHighlightingAttributeIds.BRUTAL_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE,
        Languages = CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.NONE)]
    public class BrutalImplicitlyUsedIdentifierHighlighting : IHighlighting, IBrutalIndicatorHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public BrutalImplicitlyUsedIdentifierHighlighting(DocumentRange documentRange)
        {
            myDocumentRange = documentRange;
        }

        public bool IsValid() => true;

        public DocumentRange CalculateRange() => myDocumentRange;

        public string ToolTip => null;
        public string ErrorStripeToolTip => null;
    }
}