using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.EntsPlugin
{
    [DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = new[] {typeof(IdentifierHighlightingStage)})]
    public class ColorHighlightingStage : CSharpDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (processKind == DaemonProcessKind.VISIBLE_DOCUMENT &&
                settings.GetValue(HighlightingSettingsAccessor.ColorUsageHighlightingEnabled))
            {
                return new ColorHighlighterProcess(file.GetSolution().GetComponents<IColorReferenceProvider>(), process, settings, file);
            }
            return null;
        }

        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }
}