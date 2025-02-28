using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.Util.Media;

namespace RW.Brutal
{
    /// <summary>
    ///     Responsible for mapping color names (eg. "Color.Red", "Color. Blue") to ARGB values and vice versa. Uses a
    ///     dictionary to store the mappings + look up color names and their associated RGBA values.
    /// </summary>
    public static class BrutalNamedColors
    {
        // Corresponding RGBA values are stored as `uint`s
        private static readonly Dictionary<string, uint> NamedColors =
            new()
            {
                { "Red", 0xFF0000 },
                { "Green", 0x00FF00 },
                { "Blue", 0x0000FF },
                { "White", 0xFFFFFFFF },
                { "Black", 0x000000 },
                { "Yellow", 0xFFFF00 },
                { "Cyan", 0x00FFFF },
                { "Magenta", 0xFF00FF },
                // { "Grey", 0x7F7F7FFF },
                // { "Clear", 0x00000000 }
            };

        /// <summary>
        ///     Retrieves a `JetRgbaColor` based on a given color name.
        /// </summary>
        public static JetRgbaColor? Get(string name)
        {
            // Check if the name exists in the above NamedColors dictionary and converts the stored uint value to a
            // JetRgbaColor.
            if (name != null && NamedColors.TryGetValue(name, out var value))
                return ToColor(value);
            return null;
        }

        /// <summary>
        ///     Retrieves a collection of all the named colors as IColorElement objects.
        /// </summary>
        public static IEnumerable<IColorElement> GetColorTable()
        {
            // Iterate over each named color and yield return a new ColorElement
            foreach (var namedColor in NamedColors)
            {
                yield return new ColorElement(ToColor(namedColor.Value), namedColor.Key);
            }
        }

        /// <summary>
        ///     Extracts RGB values from uint hex argument via rgb. Add back in alpha later.
        /// </summary>
        private static JetRgbaColor ToColor(uint color)
        {
            var value = color;
            
            return JetRgbaColor.FromRgb(
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)(value));
        }

        /// <summary>
        ///     Retrieves the name of a color if it exists in the above NamedColors dictionary.
        /// </summary>
        public static string GetColorName(JetRgbaColor color)
        {
            // Iterate over each named color in the dictionary
            foreach (var namedColor in NamedColors)
            {
                // Compare the stored color value (converted to JetRgbaColor) with the input color
                if (ToColor(namedColor.Value) == color)
                    return namedColor.Key;
            }

            return null;
        }
    }
}