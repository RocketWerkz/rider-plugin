using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace ReSharperPlugin.EntsPlugin
{
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
        }

        [CanBeNull] public ITypeElement ColorType { get; }
        [CanBeNull] public ITypeElement Color32Type { get; }

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