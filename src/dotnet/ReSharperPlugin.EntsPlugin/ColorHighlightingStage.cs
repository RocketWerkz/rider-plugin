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
    // `Instantiation.DemandAnyThreadSafe` specifies that this daemon stage can be instantiated in a thread-safe manner
    // `CSharpDaemonStageBase` is a daemon stage specifically for analyzing C# files
    [DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = new[] {typeof(IdentifierHighlightingStage)})]
    public class ColorHighlightingStage : CSharpDaemonStageBase
    {
        /// <summary>
        ///     Creates the Daemon Stage Process, which performs the actual analysis or highlighting.
        /// </summary>
        // <param name="process">The current daemon process that is analyzing the file.</param>
        // <param name="settings">Settings for the current context (eg. highlighting preferences).</param>
        // <param name="processKind">Specifies what type of analysis is being performed (eg. visible document analysis).</param>
        // <param name="file">The C# file being analyzed.</param>
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
            // Determines whether this daemon stage should run on the given source file
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            // Ensure that the file belongs to the C# language
            return sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }
}