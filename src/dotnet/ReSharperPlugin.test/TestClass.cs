using JetBrains.Annotations;
using JetBrains.Application.UI.Icons.Shell;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Resx.Utils;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace ReSharperPlugin.test;

[Language(typeof(CSharpLanguage))]
public class TestClass : CSharpItemsProviderBase<CSharpCodeCompletionContext>
{
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        if (!IsAvailable(context))
            return false;

        var project = context.NodeInFile.GetProject();
        if (project is null)
            return false;

        //return Logger.CatchSilent(() =>
        {
            var projectPath = project.ProjectLocationLive.Value;
            var stringLiteral = context.ToString();

            var originalString = string.Empty;
            if (stringLiteral is { } os)
            {
                originalString = os;
            }

            var relativePathString = string.Empty;
            if (originalString.StartsWith("hello"))
            {
                relativePathString = originalString.Substring(5);
            }

            var searchPath = VirtualFileSystemPath.ParseRelativelyTo(relativePathString, projectPath);

            var lookupItem = new ResourcePathItem(projectPath,
                new CompletionItem(false, projectPath, searchPath, string.Empty), context.CompletionRanges);
            collector.Add(lookupItem);
            return true;
        }
        //);
    }
}

class CompletionItem
{
    public readonly bool IsDirectory;
    public readonly VirtualFileSystemPath OriginalFolder;
    public readonly VirtualFileSystemPath Completion;
    public readonly string ExtensionWithDot;

    public CompletionItem(bool isDirectory, VirtualFileSystemPath originalFolder, VirtualFileSystemPath completion,
        string extensionWithDot)
    {
        OriginalFolder = originalFolder;
        IsDirectory = isDirectory;
        Completion = completion;
        ExtensionWithDot = extensionWithDot;
    }
}

sealed class ResourcePathItem : TextLookupItemBase
{
    private readonly CompletionItem myCompletionItem;

    public ResourcePathItem(VirtualFileSystemPath projectPath,
        CompletionItem completionItem, TextLookupRanges ranges)
    {
        myCompletionItem = completionItem;
        Ranges = ranges;
        Text =
            $"\"foo/{completionItem.Completion.MakeRelativeTo(projectPath).NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)}\"";
    }

    protected override RichText GetDisplayName() => LookupUtil.FormatLookupString(
        myCompletionItem.Completion.MakeRelativeTo(myCompletionItem.OriginalFolder)
            .NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix), TextColor);

    public override IconId Image => myCompletionItem.IsDirectory
        ? ProjectModelThemedIcons.Directory.Id
        : ShellFileIcon.Create(myCompletionItem.ExtensionWithDot);

    protected override void OnAfterComplete(ITextControl textControl, ref DocumentRange nameRange,
        ref DocumentRange decorationRange,
        TailType tailType, ref Suffix suffix, ref IRangeMarker caretPositionRangeMarker)
    {
        base.OnAfterComplete(textControl, ref nameRange, ref decorationRange, tailType, ref suffix,
            ref caretPositionRangeMarker);
        // Consistently move caret to end of path; i.e., end of the string literal, before closing quote
        textControl.Caret.MoveTo(Ranges.ReplaceRange.StartOffset + Text.Length - 1,
            CaretVisualPlacement.DontScrollIfVisible);
    }

    public override void Accept(
        ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType,
        Suffix suffix, ISolution solution, bool keepCaretStill)
    {
        // Force replace + keep caret still in order to place caret at consistent position (see override of OnAfterComplete)
        base.Accept(textControl, nameRange, LookupItemInsertType.Replace, suffix, solution, true);
    }
}

sealed class StringLiteralItem : TextLookupItemBase, IMLSortingAwareItem
{
    public StringLiteralItem([NotNull] string text)
    {
        Text = text;
    }

    public override IconId Image => PsiSymbolsThemedIcons.Const.Id;

    public override MatchingResult Match(PrefixMatcher prefixMatcher)
    {
        var matchingResult = prefixMatcher.Match(Text);
        if (matchingResult == null)
            return null;
        return new MatchingResult(matchingResult.MatchedIndices, matchingResult.AdjustedScore - 100,
            matchingResult.OriginalScore);
    }

    public override void Accept(
        ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType,
        Suffix suffix, ISolution solution, bool keepCaretStill)
    {
        base.Accept(textControl, nameRange, LookupItemInsertType.Replace, suffix, solution, keepCaretStill);
    }

    public bool UseMLSort() => false;
}