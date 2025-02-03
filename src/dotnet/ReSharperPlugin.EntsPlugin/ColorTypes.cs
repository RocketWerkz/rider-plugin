using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace ReSharperPlugin.EntsPlugin
{
    /// <summary>
    /// Helps manage and cache color-related type elements.
    /// </summary>
    public class ColorTypes
    {
        private static readonly Key<ColorTypes> ourColorTypesKey = new Key<ColorTypes>("ColorTypes");

        public static ColorTypes GetInstance(IPsiModule module)
        {
            var colorTypes = module.GetData(ourColorTypesKey);
            if (colorTypes == null)
            {
                colorTypes = new ColorTypes(module);
                module.PutData(ourColorTypesKey, colorTypes);
            }

            return colorTypes;
        }

        private ColorTypes([NotNull] IPsiModule module)
        {
            var cache = module.GetPsiServices().Symbols.GetSymbolScope(module, true, true);
            
            ColorType = cache.GetTypeElementByCLRName(KnownTypes.Float4);
            Color32Type = cache.GetTypeElementByCLRName(KnownTypes.Float4);
        }

        // References to color-related type `UnityEngine.Color` and `UnityEngine.Color32`
        [CanBeNull] public ITypeElement ColorType { get; }
        [CanBeNull] public ITypeElement Color32Type { get; }

        /// <summary>
        ///     Checks if `typeElement` is either `ColorType` or `Color32Type`.
        /// </summary>
        public bool IsColorType([CanBeNull] ITypeElement typeElement)
        {
            return (ColorType != null && ColorType.Equals(typeElement))
                || (Color32Type != null && Color32Type.Equals(typeElement));
        }

        public bool IsColorTypeSupportingProperties([CanBeNull] ITypeElement typeElement)
        {
            return ColorType != null && ColorType.Equals(typeElement);
        }
        
        public static Pair<ITypeElement, ITypeMember>? PropertyFromColorElement(ITypeElement qualifierType, IColorElement colorElement, IPsiModule module)
        {
            // Uses `BrutalNamedColors.cs` to get the color's name (eg. "Red", "Blue")
            var colorName = BrutalNamedColors.GetColorName(colorElement.RGBColor);
            if (string.IsNullOrEmpty(colorName))
                return null;

            var colorType = GetInstance(module).ColorType;
            if (colorType == null || !colorType.Equals(qualifierType)) return null;

            var colorProperties = GetStaticColorProperties(colorType);
            var propertyTypeMember = colorProperties.FirstOrDefault(p => p.ShortName == colorName);
            if (propertyTypeMember == null) return null;

            return Pair.Of(colorType, propertyTypeMember);
        }

        private static IList<ITypeMember> GetStaticColorProperties(ITypeElement colorType)
        {
            var colorProperties = new LocalList<ITypeMember>();

            foreach (var typeMember in colorType.GetMembers())
            {
                if (!typeMember.IsStatic) continue;

                var typeOwner = typeMember as ITypeOwner;
                if (typeOwner is IProperty || typeOwner is IField)
                {
                    var declaredType = typeOwner.Type as IDeclaredType;
                    if (declaredType != null && colorType.Equals(declaredType.GetTypeElement()))
                    {
                        colorProperties.Add(typeMember);
                    }
                }
            }

            return colorProperties.ResultingList();
        }
    }
}