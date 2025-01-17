#nullable enable
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.EntsPlugin;

public interface IColorReferenceProvider
{
    IColorReference? GetColorReference(ITreeNode element);
}