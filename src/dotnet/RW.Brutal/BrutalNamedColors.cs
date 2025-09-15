using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.Util.Media;

namespace RW.Brutal;

/// <summary>
///     Responsible for mapping color names (eg. "Color.Red", "Color. Blue") to ARGB values and vice versa. Uses a
///     dictionary to store the mappings + look up color names and their associated RGBA values.
/// </summary>
public static class BrutalNamedColors
{
    // Corresponding RGBA values are stored as uint hex values
    private static readonly Dictionary<string, uint> NamedColorsByte3Hex =
        new()
        {
            { "Red", 0xFF0000 },
            { "Green", 0x00FF00 },
            { "Blue", 0x0000FF },
            { "White", 0xFFFFFFFF },
            { "Black", 0x000000 },
            { "Yellow", 0xFFFF00 },
            { "Cyan", 0x00FFFF },
            { "Magenta", 0xFF00FF }
        };
        
    // Corresponds to BRUTAL Grey = float4.Rgba(0.5f, 0.5f, 0.5f);
    private static readonly Dictionary<string, (float, float, float)> NamedColorsFloat =
        new()
        {
            { "Gray", (0.5f, 0.5f, 0.5f) }
        };
        
    private static readonly Dictionary<string, uint> NamedColorsByte4Hex =
        new()
        {
            { "Clear", 0x000000000 }
        };

    /// <summary>
    ///     Retrieves a `JetRgbaColor` based on a given color name.
    /// </summary>
    public static JetRgbaColor? Get(string name)
    {
        // Check if the name exists in any of the above NamedColors dictionaries
        if (name == null) return null;
            
        // Converts the stored uint hex value to a JetRgbaColor
        if (NamedColorsByte3Hex.TryGetValue(name, out var hexValue))
            return ToColorHex(hexValue);
            
        // Same but for float values
        if (NamedColorsFloat.TryGetValue(name, out var floatValue))
            return ToColorFloat(floatValue);
            
        // Same but for Byte4 hex values
        if (NamedColorsByte4Hex.TryGetValue(name, out var hexAlphaValue))
            return ToColorHexAlpha(hexAlphaValue);
            
        return null;
    }

    /// <summary>
    ///     Retrieves a collection of all the named colors as IColorElement objects.
    /// </summary>
    public static IEnumerable<IColorElement> GetColorTable()
    {
        // Iterate over each named color and yield return a new ColorElement
        foreach (var namedColor in NamedColorsByte3Hex)
            yield return new ColorElement(ToColorHex(namedColor.Value), namedColor.Key);
            
        foreach (var namedColor in NamedColorsFloat)
            yield return new ColorElement(ToColorFloat(namedColor.Value), namedColor.Key);
            
        foreach (var namedColor in NamedColorsByte4Hex)
            yield return new ColorElement(ToColorHexAlpha(namedColor.Value), namedColor.Key);
    }

    /// <summary>
    ///     Extracts RGB values from uint hex argument via rgb, assume alpha is always 255.
    /// </summary>
    private static JetRgbaColor ToColorHex(uint color)
    {
        const byte a = 255;
        byte r = (byte)(color >> 16);
        byte g = (byte)(color >> 8);
        byte b = (byte)color;
        return JetRgbaColor.FromArgb(a, r, g, b);
    }
        
    /// <summary>
    ///     Extracts RGB values from float arguments via rgb (eg. `float4.Rgba(0.5f, 0.5f, 0.5f)`).
    /// </summary>
    private static JetRgbaColor ToColorFloat((float r, float g, float b) color)
    {
        const byte a = 255;
        byte r = (byte)(255 * color.r);
        byte g = (byte)(255 * color.g);
        byte b = (byte)(255 * color.b);
        return JetRgbaColor.FromArgb(a, r, g, b);
    }
        
    /// <summary>
    ///     Extracts RGBA values from uint hex argument via rgba, assume alpha is always included.
    /// </summary>
    private static JetRgbaColor ToColorHexAlpha(uint color)
    {
        byte a = (byte)color;
        byte r = (byte)(color >> 24);
        byte g = (byte)(color >> 16);
        byte b = (byte)(color >> 8);
        return JetRgbaColor.FromArgb(a, r, g, b);
    }

    /// <summary>
    ///     Retrieves the name of a color if it exists in any of the above NamedColors dictionaries.
    /// </summary>
    public static string GetColorName(JetRgbaColor color)
    {
        // Iterate over each named color in the dictionary
        foreach (var namedColor in NamedColorsByte3Hex)
        {
            // Compare the stored color value (converted to JetRgbaColor) with the input color
            if (ToColorHex(namedColor.Value) == color)
                return namedColor.Key;
        }
            
        foreach (var namedColor in NamedColorsFloat)
        {
            if (ToColorFloat(namedColor.Value) == color)
                return namedColor.Key;
        }

        foreach (var namedColor in NamedColorsByte4Hex)
        {
            if (ToColorHexAlpha(namedColor.Value) == color)
                return namedColor.Key;
        }

        return null;
    }
}