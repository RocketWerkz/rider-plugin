#nullable enable
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Tree;

namespace RW.Brutal;

public interface IColorReferenceProvider
{
    IColorReference? GetColorReference(ITreeNode element);
}