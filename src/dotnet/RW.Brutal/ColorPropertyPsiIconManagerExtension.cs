using JetBrains.Application.UI.Icons.ColorIcons;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

namespace RW.Brutal
{
    /// <summary>
    ///     Assigns color icons to Brutal named colors for autocomplete dropdown list.
    /// </summary>
    [DeclaredElementIconProvider]
    public class ColorPropertyPsiIconManagerExtension : IDeclaredElementIconProvider
    {
        public IconId GetImageId(IDeclaredElement declaredElement, PsiLanguageType languageType,
            out bool canApplyExtensions)
        {
            canApplyExtensions = false;

            if(declaredElement is not ITypeMember typeMember) return null;

            // Get color name to create the icon
            var color = BrutalNamedColors.Get(typeMember.ShortName);
            return color == null ? null : new ColorIconId(color.Value);
        }
    }
}