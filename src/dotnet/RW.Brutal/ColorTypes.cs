using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace RW.Brutal;

/// <summary>
///     Helps manage and cache color-related type elements.
/// </summary>
public class ColorTypes
{
    private static readonly Key<ColorTypes> ourColorTypesKey = new("ColorTypes");

    public static ColorTypes GetInstance(IPsiModule module)
    {
        // var colorTypes = module.GetData(ourColorTypesKey);
        // if (colorTypes is not null)
        //     return colorTypes;
            
        // BUG-FIX: need to create ColorTypes each time
        // Gets around issue where IDE goes into Debug mode and daemon changes process
        // giving each property a different instance type making IsColorType() not return correct value
        // - Brogan 2025-09-15
        var colorTypes = new ColorTypes(module);
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
        var isFloat3 = ColorFloat3Type != null && ColorFloat3Type.Equals(typeElement);
        var isFloat4 = ColorFloat4Type != null && ColorFloat4Type.Equals(typeElement);
        var isByte3 = ColorByte3Type != null && ColorByte3Type.Equals(typeElement);
        var isByte4 = ColorByte4Type != null && ColorByte4Type.Equals(typeElement);
        var isUshort3 = ColorUshort3Type != null && ColorUshort3Type.Equals(typeElement);
        var isUshort4 = ColorUshort4Type != null && ColorUshort4Type.Equals(typeElement);
        var isColor = ColorType != null && ColorType.Equals(typeElement);
        return isFloat3
               || isFloat4 
               || isByte3
               || isByte4
               || isUshort3
               || isUshort4
               || isColor;
    }
        
    /// <summary>
    ///     Retrieves a property representing a color element from a given color type.
    /// </summary>
    public static Pair<ITypeElement, ITypeMember>? PropertyFromColorElement(ITypeElement qualifierType, IColorElement colorElement, IPsiModule module)
    {
        // Get the color name from the RGB values of the color element
        var colorName = BrutalNamedColors.GetColorName(colorElement.RGBColor);
        if (string.IsNullOrEmpty(colorName))
            return null;

        // Retrieve the color type instance (assumed to be a Float4 type)
        var colorType = GetInstance(module).ColorFloat4Type;
        if (colorType == null || !colorType.Equals(qualifierType)) return null;

        // Find the property that matches the color name
        var colorProperties = GetStaticColorProperties(colorType);
        var propertyTypeMember = colorProperties.FirstOrDefault(p => p.ShortName == colorName);
        if (propertyTypeMember == null) return null;

        return Pair.Of(colorType, propertyTypeMember);
    }

    /// <summary>
    ///     Retrieves all static properties from a given color type that represent color values.
    /// </summary>
    private static IList<ITypeMember> GetStaticColorProperties(ITypeElement colorType)
    {
        var colorProperties = new LocalList<ITypeMember>();

        // Iterate through all members of the color type
        foreach (var typeMember in colorType.GetMembers())
        {
            if (!typeMember.IsStatic) continue;

            // Check if the member is a property or field
            var typeOwner = typeMember as ITypeOwner;
            if (typeOwner is IProperty || typeOwner is IField)
            {
                // Ensure the member's type matches the color type
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