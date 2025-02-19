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
        private static readonly Key<ColorTypes> ourColorTypesKey = new("ColorTypes");

        public static ColorTypes GetInstance(IPsiModule module)
        {
            var colorTypes = module.GetData(ourColorTypesKey);
            if (colorTypes != null) return colorTypes;
            colorTypes = new ColorTypes(module);
            module.PutData(ourColorTypesKey, colorTypes);

            return colorTypes;
        }

        private ColorTypes([NotNull] IPsiModule module)
        {
            var cache = module.GetPsiServices().Symbols.GetSymbolScope(module, true, true);
            
            ColorFloat3Type = cache.GetTypeElementByCLRName(KnownTypes.Float3);
            ColorFloat4Type = cache.GetTypeElementByCLRName(KnownTypes.Float4);
            
            ColorByte3Type = cache.GetTypeElementByCLRName(KnownTypes.Byte3);
            ColorByte4Type = cache.GetTypeElementByCLRName(KnownTypes.Byte4);
            
            ColorUshort3Type = cache.GetTypeElementByCLRName(KnownTypes.Ushort3);
            ColorUshort4Type = cache.GetTypeElementByCLRName(KnownTypes.Ushort4);
            
            ColorType = cache.GetTypeElementByCLRName(KnownTypes.Color);
        }

        // Naming was previously references to color-related type `UnityEngine.Color` and `UnityEngine.Color32` only
        [CanBeNull] public ITypeElement ColorFloat3Type { get; }
        [CanBeNull] public ITypeElement ColorFloat4Type { get; }
        [CanBeNull] public ITypeElement ColorByte3Type { get; }
        [CanBeNull] public ITypeElement ColorByte4Type { get; }
        [CanBeNull] public ITypeElement ColorUshort3Type { get; }
        [CanBeNull] public ITypeElement ColorUshort4Type { get; }
        [CanBeNull] public ITypeElement ColorType { get; }

        /// <summary>
        ///     Checks if `typeElement` is any of the types above.
        /// </summary>
        public bool IsColorType([CanBeNull] ITypeElement typeElement)
        {
            return (ColorFloat3Type != null && ColorFloat3Type.Equals(typeElement))
                   || (ColorFloat4Type != null && ColorFloat4Type.Equals(typeElement))
                   || (ColorByte3Type != null && ColorByte3Type.Equals(typeElement))
                   || (ColorByte4Type != null && ColorByte4Type.Equals(typeElement))
                   || (ColorUshort3Type != null && ColorUshort3Type.Equals(typeElement))
                   || (ColorUshort4Type != null && ColorUshort4Type.Equals(typeElement))
                   || (ColorType != null && ColorType.Equals(typeElement));
        }
        
        /// <summary>
        ///     Checks if `ColorFloat4Type` supports color properties (used for color name mappings?)
        /// </summary>
        public bool IsColorTypeSupportingProperties([CanBeNull] ITypeElement typeElement)
        {
            return ColorFloat4Type != null && ColorFloat4Type.Equals(typeElement);
        }
        
        // Temp method used for color name mappings
        public static Pair<ITypeElement, ITypeMember>? PropertyFromColorElement(ITypeElement qualifierType, IColorElement colorElement, IPsiModule module)
        {
            var colorName = BrutalNamedColors.GetColorName(colorElement.RGBColor);
            if (string.IsNullOrEmpty(colorName))
                return null;

            var colorType = GetInstance(module).ColorFloat4Type;
            if (colorType == null || !colorType.Equals(qualifierType)) return null;

            var colorProperties = GetStaticColorProperties(colorType);
            var propertyTypeMember = colorProperties.FirstOrDefault(p => p.ShortName == colorName);
            if (propertyTypeMember == null) return null;

            return Pair.Of(colorType, propertyTypeMember);
        }

        // Temp method used for color name mappings
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