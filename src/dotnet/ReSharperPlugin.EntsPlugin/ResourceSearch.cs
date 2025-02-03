using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Icons.Shell;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
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
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using ReSharperPlugin.EntsPlugin.Completions;

namespace ReSharperPlugin.EntsPlugin;

[Language(typeof(CSharpLanguage))]
public class ResourceSearch : CSharpItemsProviderBase<CSharpCodeCompletionContext>
{
    private String Prefix = "Content/";
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion;
    }
    
    private static readonly Dictionary<IClrTypeName, IList<string>> ourFileExtensionsByType;
    
    private IEnumerable<CompletionItem> FullPathCompletions(CSharpCodeCompletionContext context, VirtualFileSystemPath searchPath)
    {
        if (context.GetResourceType() is not { } resourceType)
            return Enumerable.Empty<CompletionItem>();

        ourFileExtensionsByType.TryGetValue(resourceType, out var matchingFileExtensions);
        return matchingFileExtensions is null 
            ? Enumerable.Empty<CompletionItem>()
            : ResourceFiles(searchPath, matchingFileExtensions);
    }
    
    private IEnumerable<CompletionItem> ResourceFiles(VirtualFileSystemPath path, IList<string> extensions)
    {
        var searchDir = SearchDir(path);
        if (searchDir is null)
        {
            return Enumerable.Empty<CompletionItem>();
        }

        return
            from p in ResourceFilesInner(searchDir, extensions)
            select new CompletionItem(p.ExistsDirectory, searchDir,p, p.ExtensionWithDot) ;
    }
    
    private static IEnumerable<VirtualFileSystemPath> ResourceFilesInner(VirtualFileSystemPath path, IList<string> extensions)
    {
        if (ShouldIgnore(path))
        {
            return Enumerable.Empty<VirtualFileSystemPath>();
        }

        if (path.ExistsFile && extensions.Any(ext => ext.Equals(path.ExtensionNoDot, StringComparison.OrdinalIgnoreCase)))
        {
            return new[] { path };
        }

        if (path.ExistsDirectory)
        {
            return
                path.GetChildren()
                    .SelectMany(child => ResourceFilesInner(child.GetAbsolutePath(), extensions));
        }

        return Enumerable.Empty<VirtualFileSystemPath>();
    }
    
    private static bool ShouldIgnore(VirtualFileSystemPath path)
    {
        // Do not check or suggest:
        // - dotfiles or directories starting with "."
        // - iml - some subsidiary file, which Rider creates
        return path.Name.StartsWith(".")
               || "import".Equals(path.ExtensionNoDot, StringComparison.OrdinalIgnoreCase)
               || "iml".Equals(path.ExtensionNoDot, StringComparison.OrdinalIgnoreCase)
               || path.ExtensionNoDot.Equals("csproj", StringComparison.OrdinalIgnoreCase)
               || path.ExtensionNoDot.Equals("sln", StringComparison.OrdinalIgnoreCase);
    }
    
    private static VirtualFileSystemPath SearchDir(VirtualFileSystemPath path)
    {
        switch (path.Exists)
        {
            case FileSystemPath.Existence.Directory: return path;
            case FileSystemPath.Existence.Missing:   return path.Parent;
            default:                                 return null;
        }
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
            if (projectPath is null) 
                return false;
            
            var stringLiteral = context.StringLiteral();
            if (stringLiteral is null)
                return false;

            var originalString = string.Empty;
            if (stringLiteral.ConstantValue.AsString() is { } os)
            {
                originalString = os;
            }
            
            // Ensure relative path starts from "Content/" prefix
            var relativePathString = string.Empty;
            if (originalString.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                relativePathString = originalString.Substring(Prefix.Length);
            
            // Start searching from the "Content" folder
            var contentPath = projectPath.Combine("Content");
            var searchPath = VirtualFileSystemPath.ParseRelativelyTo(relativePathString, contentPath);

            var completions = FullPathCompletions(context, searchPath).ToList();

            // If path leads outside the "Content" folder, skip completions
            if (!contentPath.IsPrefixOf(searchPath))
            {
                return false;
            }

            if (originalString.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                completions.AddRange(OneLevelPathCompletions(searchPath));
            }
                
            // Create ResourcePathItem for each completion
            var items = completions.Distinct().Select(completion =>
                new ResourcePathItem(contentPath, completion, context.CompletionRanges)).ToList();
            
            foreach (var item in items)
            {
                collector.Add(item);
            }
                
            if (!originalString.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && completions.Any())
            {
                // Add a fallback item with the "Content/" prefix for cases where no completions match
                var resItem = new StringLiteralItem(Prefix);
                var ranges = context.CompletionRanges;
                var range = new TextLookupRanges(
                    new DocumentRange(ranges.InsertRange.StartOffset + 1, ranges.InsertRange.EndOffset + 1),
                    new DocumentRange(ranges.ReplaceRange.StartOffset + 1, ranges.ReplaceRange.EndOffset - 1)
                );

                resItem.InitializeRanges(range, context.BasicContext);
                collector.Add(resItem);
            }
            return !items.IsEmpty();
        }
        //);
    }
    
    private IEnumerable<CompletionItem> OneLevelPathCompletions(VirtualFileSystemPath path)
    {
        var searchDir = SearchDir(path);
        if (searchDir is null)
        {
            return Enumerable.Empty<CompletionItem>();
        }

        return
            from child in searchDir.GetChildren()
            where !ShouldIgnore(child.GetAbsolutePath())
            select new CompletionItem(child.IsDirectory, searchDir, child.GetAbsolutePath(), child.GetAbsolutePath().ExtensionWithDot);
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
        
        // Force projectPath to point to root/Content
        var contentPath = projectPath.Combine("Content");
        
        // Compute relative path and enforce "Content/" prefix
        var relativePath = completionItem.Completion.MakeRelativeTo(contentPath)
            .NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix);
        
        if (!relativePath.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            relativePath = $"Content/{relativePath.TrimStart('.', '/')}";
        
        Text = $"\"{relativePath}\"";
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