using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.ColorHints;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Media;

#nullable enable

namespace RW.Brutal
{
    /// <summary>
    ///     `CSharpIncrementalDaemonStageProcessBase` is a base class for analyzing C# code in small chunks (incremental
    ///     processing). Highlights color-related expressions (eg. `float4.Rgba(r, g, b, a`) in the editor.
    /// </summary>
    public class ColorHighlighterProcess : CSharpIncrementalDaemonStageProcessBase
    {
        private readonly IEnumerable<IColorReferenceProvider> myProviders;

        /// <summary>
        ///     Constructor that initializes the process and stores the providers for later use in identifying color
        ///     references.
        /// </summary>
        // <param name="providers">A collection of objects (`IColorReferenceProvider`) that help resolve color references in code.</param>
        // <param name="process">Represents the daemon process handling the analysis.</param>
        // <param name="settingsStore">Provides access to plugin-specific settings.</param>
        // <param name="file">The C# file being analyzed.</param>
        public ColorHighlighterProcess(IEnumerable<IColorReferenceProvider> providers, IDaemonProcess process,
            IContextBoundSettingsStore settingsStore, ICSharpFile file)
            : base(process, settingsStore, file)
        {
            myProviders = providers;
        }

        public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is ITokenNode tokenNode && tokenNode.GetTokenType().IsWhitespace) return;

            var colorInfo = CreateColorHighlightingInfo(element, myProviders);
            
            // If a valid color is found, add a highlight to the editor
            if (colorInfo != null)
                consumer.AddHighlighting(colorInfo.Highlighting, colorInfo.Range);
        }

        private HighlightingInfo? CreateColorHighlightingInfo(ITreeNode element, IEnumerable<IColorReferenceProvider> providers)
        {
            var colorReference = GetColorReference(element, providers);
            var range = colorReference?.ColorConstantRange;
            return range?.IsValid() == true
                ? new HighlightingInfo(range.Value, new ColorHintHighlighting(colorReference))
                : null;
        }
        
        /// <summary>
        ///     Attempts to retrieve a color reference and ensures only one reference is returned (so there are no
        ///     multiple color icon displays).
        /// </summary>
        private static IColorReference? GetColorReference(ITreeNode element, IEnumerable<IColorReferenceProvider> providers)
        {
            // Checks if an invocation expression (eg. `float4.Rgba(r, g, b, a)` or `Color.red`)
            var referenceExpression = element as IReferenceExpression;
            if (referenceExpression?.QualifierExpression is IReferenceExpression qualifier)
            {
                var reference = ReferenceFromInvocation(qualifier, referenceExpression)
                                ?? ReferenceFromProperty(qualifier, referenceExpression);
                
                if (reference != null)
                    return reference;
            }

            // Fallback which checks additional providers only if no reference was found above
            foreach (var provider in providers)
            {
                var reference = provider.GetColorReference(element);
                if (reference != null)
                    return reference;
            }
            
            return null;
        }

        /// <summary>
        ///     Handles color references created via an invocation. Eg. `float4.Rgb(r, g, b)`,
        ///     `byte4.Rgb(r, g, b)` and `ushort4.Rgb(r, g, b)`.
        /// </summary>
        private static IColorReference? ReferenceFromInvocation(IReferenceExpression qualifier,
            IReferenceExpression methodReferenceExpression)
        {
            var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(methodReferenceExpression);
            if (invocationExpression == null || invocationExpression.Arguments.IsEmpty)
                return null;

            var methodReference = methodReferenceExpression.Reference;

            // Extract the name of the method being invoked and ensure it matches (case-sensitive check)
            var name = methodReference.GetName();

            // Check # of arguments so correct method name and color type is used
            var arguments = invocationExpression.Arguments;
            bool isOneArg = arguments.Count == 1;
            bool isTwoArgs = arguments.Count == 2;
            bool isThreeArgs = arguments.Count == 3;
            bool isFourArgs = arguments.Count == 4;
            if (!isOneArg && !isTwoArgs && !isThreeArgs && !isFourArgs)
                return null;

            // Unwind the argument values into a string for logging purposes
            // var argValues = string.Join(", ", arguments.Select(arg => arg.Value?.GetText()));
            // Log.Root.Error($"argValues: {argValues}");

            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType is null) return null;

            var colorTypes = ColorTypes.GetInstance(qualifierType.Module);
            if (!colorTypes.IsColorType(qualifierType)) return null;

            // Validate method name based on color type and argument count
            if (isOneArg)
            {
                // Handle float3, float4, ushort4, ushort3 "Grayscale"
                if (colorTypes.ColorFloat4Type is not null && colorTypes.ColorFloat4Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                else if (colorTypes.ColorFloat3Type is not null && colorTypes.ColorFloat3Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                else if (colorTypes.ColorUshort4Type is not null && colorTypes.ColorUshort4Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                else if (colorTypes.ColorUshort3Type is not null && colorTypes.ColorUshort3Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                
                // byte3 and byte4 can both use "HexColor" and "Grayscale" when given one argument
                else if (colorTypes.ColorByte3Type is not null && colorTypes.ColorByte3Type.Equals(qualifierType))
                    if (!string.Equals(name, "HexColor", StringComparison.Ordinal) ||
                        !string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                else if (colorTypes.ColorByte4Type is not null && colorTypes.ColorByte4Type.Equals(qualifierType))
                    if (!string.Equals(name, "HexColor", StringComparison.Ordinal) ||
                        !string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
            }
            else if (isTwoArgs)
            {
                if (colorTypes.ColorFloat4Type is not null && colorTypes.ColorFloat4Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                else if (colorTypes.ColorUshort4Type is not null && colorTypes.ColorUshort4Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
                else if (colorTypes.ColorByte4Type is not null && colorTypes.ColorByte4Type.Equals(qualifierType))
                    if (!string.Equals(name, "Grayscale", StringComparison.Ordinal)) return null;
            }
            else
            {
                // Other types follow Rgb (three args) and Rgba (four args) convention
                if (!((isThreeArgs && string.Equals(name, "Rgb", StringComparison.Ordinal)) ||
                      (isFourArgs && string.Equals(name, "Rgba", StringComparison.Ordinal)))) return null;
            }
            
            // Checks if the type matches any of the known color types
            JetRgbaColor? color = null;
            
            // Float4 supports 1 to 4 arguments + "Rgb", "Rgba" and "Grayscale" method names
            if (colorTypes.ColorFloat4Type is not null && colorTypes.ColorFloat4Type.Equals(qualifierType))
            {
                // Handle Grayscale with a single arg with full opacity (alpha is always 1)
                if (isOneArg)
                {
                    var baseColor = GetColorFromFloatGrayscale(arguments);
                    if (baseColor is null) return null;
                    var (_, rgb) = baseColor.Value;
                    color = rgb.WithA(255);
                }
                // Handle Grayscale with two args
                else if (isTwoArgs)
                {
                    var baseColor = GetColorFromFloatGrayscale(arguments);
                    if (baseColor is null) return null;
                    var (a, rgb) = baseColor.Value;
                    color = rgb.WithA((byte)(255.0 * a.Value));
                }
                else
                {
                    // Attempt to parse color from floating-point RGBA
                    var baseColor = GetColorFromFloatRgba(arguments);
                    if (baseColor is null) return null;

                    // If an alpha value exists, adjust the color's transparency accordingly, otherwise default to full
                    // opacity (1)
                    var (a, rgb) = baseColor.Value;
                    color = rgb.WithA(isThreeArgs ? (byte)255 : (byte)(255.0 * a.Value));
                }
            }
            
            else if (colorTypes.ColorFloat3Type is not null && colorTypes.ColorFloat3Type.Equals(qualifierType))
            {
                // Handle Grayscale with a single arg with full opacity default (alpha is always 1)
                // Clear shows up as a black icon
                var baseColor = isOneArg ? GetColorFromFloatGrayscale(arguments) : GetColorFromFloatRgba(arguments);
                if (baseColor is null) return null;
                var (_, rgb) = baseColor.Value;
                color = rgb.WithA(255);
            }
            
            else if (colorTypes.ColorByte4Type is not null && colorTypes.ColorByte4Type.Equals(qualifierType))
            {
                // Handle HexColor and Grayscale
                if (isOneArg)
                {
                    (byte? alpha, JetRgbaColor)? baseColor;
                    if (string.Equals(name, "HexColor", StringComparison.Ordinal))
                    {
                        // Check if the "hex" argument is a valid string or a uint
                        baseColor = GetColorFromHexStringRgba(arguments) ?? GetColorFromHexColorRgba(arguments);
                        if (baseColor is null) return null;
                        var (a, hex) = baseColor.Value;
                        color = hex.WithA((byte)a);
                    }
                    else if(string.Equals(name, "Grayscale", StringComparison.Ordinal))
                    {
                        baseColor = GetColorFromByteGrayscale(arguments);
                        if (baseColor is null) return null;
                        var (_, rgb) = baseColor.Value;
                        color = rgb.WithA(255);
                    }
                }
                // Handle Grayscale with two args
                else if (isTwoArgs)
                {
                    var baseColor = GetColorFromByteGrayscale(arguments);
                    if (baseColor is null) return null;
                    var (a, rgb) = baseColor.Value;
                    color = rgb.WithA((byte)a);
                }
                else
                {
                    var baseColor = GetColorFromByteRgba(arguments);
                    if (baseColor is null) return null;
                    if (isThreeArgs)
                    {
                        var (_, rgb) = baseColor.Value;
                        color = rgb.WithA(255);
                    }
                    else if (isFourArgs)
                    {
                        var (a, rgb) = baseColor.Value;
                        color = rgb.WithA((byte)a);
                    }
                }
            }
            
            else if (colorTypes.ColorByte3Type is not null && colorTypes.ColorByte3Type.Equals(qualifierType))
            {
                (byte? alpha, JetRgbaColor)? baseColor = null;
                // Handle HexColor and Grayscale
                if (isOneArg)
                {
                    if (string.Equals(name, "HexColor", StringComparison.Ordinal))
                        // Check if the "hex" argument is a valid string or a uint
                        baseColor = GetColorFromHexStringRgb(arguments) ?? GetColorFromHexColorRgb(arguments);
                    else if (string.Equals(name, "Grayscale", StringComparison.Ordinal))
                        baseColor = GetColorFromByteGrayscale(arguments);
                }
                else if (isThreeArgs)
                    baseColor = GetColorFromByteRgba(arguments);
                if (baseColor is null) return null;
                
                // Ignore the alpha value since it doesn't exist and explicitly default set it to 255 (full opacity)
                var (_, rgb) = baseColor.Value;
                color = rgb.WithA(255);
            }
            
            else if (colorTypes.ColorUshort4Type is not null && colorTypes.ColorUshort4Type.Equals(qualifierType))
            {
                // Handle Grayscale with a single arg with full opacity (alpha is always 0xFFFF)
                if (isOneArg)
                {
                    var baseColor = GetColorFromUshortGrayscale(arguments);
                    if (baseColor is null) return null;
                    var (_, rgb) = baseColor.Value;
                    color = rgb.WithA(255);
                }
                // Handle Grayscale with two args
                else if (isTwoArgs)
                {
                    var baseColor = GetColorFromUshortGrayscale(arguments);
                    if (baseColor is null) return null;
                    var (a, rgb) = baseColor.Value;
                    color = rgb.WithA((byte)a);
                }
                else
                {
                    var baseColor = GetColorFromUshortRgba(arguments);
                    if (baseColor is null) return null;

                    // If we only have three args default set the alpha value to 255 (full opacity)
                    if (isThreeArgs)
                    {
                        var (_, rgb) = baseColor.Value;
                        color = rgb.WithA(255);
                    }
                    else if (isFourArgs)
                    {
                        var (a, rgb) = baseColor.Value;
                        color = rgb.WithA((byte)a);
                    }
                }
            }
            
            else if (colorTypes.ColorUshort3Type is not null && colorTypes.ColorUshort3Type.Equals(qualifierType))
            {
                // Handle Grayscale with a single arg with full opacity default alpha
                var baseColor = isOneArg ? GetColorFromUshortGrayscale(arguments) : GetColorFromUshortRgba(arguments); 
                if (baseColor is null) return null;
                
                // Ignore the alpha value since it doesn't exist and explicitly default set it to 255 (full opacity)
                var (_, rgb) = baseColor.Value;
                color = rgb.WithA(255);
            }

            if (color is null) return null;
            
            var colorElement = new ColorElement(color.Value);
            var argumentList = invocationExpression.ArgumentList;
            return new ColorReference(colorElement, invocationExpression, argumentList, argumentList.GetDocumentRange());
        }
        
        /// <summary>
        ///     Handles color references created from a predefined color property (eg. `Color.Red`).
        /// </summary>
        private static IColorReference? ReferenceFromProperty(IReferenceExpression qualifier,
            IReferenceExpression colorQualifiedMemberExpression)
        {
            // Get the name of the referenced property (eg., "Red" in "Color.Red")
            var name = colorQualifiedMemberExpression.Reference.GetName();

            // Look up the color name in our predefined color list
            var color = BrutalNamedColors.Get(name);
            if (color == null) return null;

            // Resolve the type of the qualifier (eg., "Color" in "Color.Red")
            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;

            var colorTypes = ColorTypes.GetInstance(qualifierType.Module);
            if (!colorTypes.IsColorType(qualifierType)) return null;
            
            var colorElement = new ColorElement(color.Value, name);
            return new ColorReference(colorElement, colorQualifiedMemberExpression,
                colorQualifiedMemberExpression, colorQualifiedMemberExpression.NameIdentifier.GetDocumentRange());
        }

        /// <summary>
        ///     Extracts RGBA values from float arguments via rgba (eg. `float4.Rgba(0.5f, 0.2f, 0.8f, 1f)`).
        /// </summary>
        private static (float? alpha, JetRgbaColor)? GetColorFromFloatRgba(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsFloatConstant(arguments, "r", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "g", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "b", 0, 1);
            var a = GetArgumentAsFloatConstant(arguments, "a", 0, 1);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)(255.0 * r.Value), (byte)(255.0 * g.Value), (byte)(255.0 * b.Value)));
        }
        
        /// <summary>
        ///     Extracts RGBA values from one or two float arguments via Grayscale.
        ///     Eg. `float4.Grayscale(0, 0)` for Clear.
        ///     Eg. `float4.Grayscale(0)` for Black.
        /// </summary>
        private static (float? alpha, JetRgbaColor)? GetColorFromFloatGrayscale(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsFloatConstant(arguments, "v", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "v", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "v", 0, 1);
            var a = GetArgumentAsFloatConstant(arguments, "a", 0, 1);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)(255.0 * r.Value), (byte)(255.0 * g.Value), (byte)(255.0 * b.Value)));
        }
        
        /// <summary>
        ///     Extracts RGBA values from byte arguments via rgba (eg. `byte4.Rgba(r, g, b, a)`).
        /// </summary>
        private static (byte? alpha, JetRgbaColor)? GetColorFromByteRgba(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsByteConstant(arguments, "r", 0, 255);
            var g = GetArgumentAsByteConstant(arguments, "g", 0, 255);
            var b = GetArgumentAsByteConstant(arguments, "b", 0, 255);
            var a = GetArgumentAsByteConstant(arguments, "a", 0, 255);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;
            return (a, JetRgbaColor.FromRgb(r.Value, g.Value, b.Value));
        }
        
        /// <summary>
        ///     Extracts RGBA values from byte arguments via Grayscale.
        ///     Eg. `byte4.Grayscale(0, 0)` for Clear.
        ///     Eg. `byte4.Grayscale(0)` for Black.
        /// </summary>
        private static (byte? alpha, JetRgbaColor)? GetColorFromByteGrayscale(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsByteConstant(arguments, "v", 0, 255);
            var g = GetArgumentAsByteConstant(arguments, "v", 0, 255);
            var b = GetArgumentAsByteConstant(arguments, "v", 0, 255);
            var a = GetArgumentAsByteConstant(arguments, "a", 0, 255);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;
            return (a, JetRgbaColor.FromRgb(r.Value, g.Value, b.Value));
        }
        
        /// <summary>
        ///     Extracts RGBA values from ushort arguments via rgba (eg. `ushort4.Rgba(r, g, b, a)`) and normalizes
        ///     them from the ushort range (0-65535) to the byte range (0-255). If ushort is directly cast to a byte,
        ///     it causes precision loss by truncating higher values. This incorrectly converts mid-range greys to
        ///     completely white. Eg. Incorrect conversion, turns grey (0.5, 0.5, 0.5) into white (1, 1, 1):
        ///         (byte)0x7FFF == (byte)32767 == 255
        ///     The correct proper conversion avoids this by scaling (this preserves the grey correctly):
        ///         (0x7FFF * 255 / 65535) == 127
        /// </summary>
        private static (ushort? alpha, JetRgbaColor)? GetColorFromUshortRgba(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsUshortConstant(arguments, "r", 0, ushort.MaxValue);
            var g = GetArgumentAsUshortConstant(arguments, "g", 0, ushort.MaxValue);
            var b = GetArgumentAsUshortConstant(arguments, "b", 0, ushort.MaxValue);
            var a = GetArgumentAsUshortConstant(arguments, "a", 0, ushort.MaxValue);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;
            
            var byteR = (byte)(r.Value * 255 / ushort.MaxValue);
            var byteG = (byte)(g.Value * 255 / ushort.MaxValue);
            var byteB = (byte)(b.Value * 255 / ushort.MaxValue);
            
            return (a, JetRgbaColor.FromRgb(byteR, byteG, byteB));
        }
        
        /// <summary>
        ///     Extracts RGBA values from one or two ushort arguments via Grayscale (Eg. `ushort4.Grayscale(0, 0)` for
        ///     Clear) and normalizes them from the ushort range (0-65535) to the byte range (0-255).
        /// </summary>
        private static (ushort? alpha, JetRgbaColor)? GetColorFromUshortGrayscale(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsUshortConstant(arguments, "v", 0, ushort.MaxValue);
            var g = GetArgumentAsUshortConstant(arguments, "v", 0, ushort.MaxValue);
            var b = GetArgumentAsUshortConstant(arguments, "v", 0, ushort.MaxValue);
            var a = GetArgumentAsUshortConstant(arguments, "a", 0, ushort.MaxValue);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;
            
            var byteR = (byte)(r.Value * 255 / ushort.MaxValue);
            var byteG = (byte)(g.Value * 255 / ushort.MaxValue);
            var byteB = (byte)(b.Value * 255 / ushort.MaxValue);
            
            return (a, JetRgbaColor.FromRgb(byteR, byteG, byteB));
        }
        
        /// <summary>
        ///     Extracts RGB values from uint hex argument via rgb using bit shifting, used for Byte3.
        /// </summary>
        private static (byte? alpha, JetRgbaColor)? GetColorFromHexColorRgb(ICollection<ICSharpArgument> arguments)
        {
            var hex = GetByte3ArgumentAsUintConstant(arguments, "hex", uint.MinValue, uint.MaxValue);
            if (!hex.HasValue)
                return null;
            byte r = (byte)(hex >> 16);
            byte g = (byte)(hex >> 8);
            byte b = (byte)hex;
            return (null, JetRgbaColor.FromRgb(r, g, b));
        }
        
        /// <summary>
        ///     Extracts RGBA values from uint hex argument via rgba using bit shifting, used for Byte4.
        /// </summary>
        private static (byte? alpha, JetRgbaColor)? GetColorFromHexColorRgba(ICollection<ICSharpArgument> arguments)
        {
            // Checks for hex values 0xFF (eg. White) first then try 0x00 (eg. Black)
            var hex = GetByte4ArgumentAsUintConstant(arguments, "hex", uint.MinValue, uint.MaxValue);
            if (!hex.HasValue)
            {
                hex = GetByte3ArgumentAsUintConstant(arguments, "hex", uint.MinValue, uint.MaxValue);
                if (!hex.HasValue)
                    return null;
            }
            byte a = (byte)hex;
            byte r = (byte)(hex >> 24);
            byte g = (byte)(hex >> 16);
            byte b = (byte)(hex >> 8);
            return (a, JetRgbaColor.FromRgb(r, g, b));
        }
        
        /// <summary>
        ///     Extracts RGB values from uint hex string argument via rgb using bit shifting, used for Byte3.
        /// </summary>
        private static (byte? alpha, JetRgbaColor)? GetColorFromHexStringRgb(ICollection<ICSharpArgument> arguments)
        {
            // Check if argument is a valid hex string first
            var hex = GetByte3StringArgumentAsUintConstant(arguments, "hex", uint.MinValue, uint.MaxValue);
            if (!hex.HasValue)
                return null;
            
            byte r = (byte)(hex >> 16);
            byte g = (byte)(hex >> 8);
            byte b = (byte)hex;
            return (null, JetRgbaColor.FromRgb(r, g, b));
        }
        
        /// <summary>
        ///     Extracts RGBA values from uint hex string argument via rgba using bit shifting, used for Byte4.
        /// </summary>
        private static (byte? alpha, JetRgbaColor)? GetColorFromHexStringRgba(ICollection<ICSharpArgument> arguments)
        {
            // Check if argument is a valid hex string first
            var hex = GetByte4StringArgumentAsUintConstant(arguments, "hex", uint.MinValue, uint.MaxValue);
            if (!hex.HasValue)
                return null;
            
            byte a = (byte)hex;
            byte r = (byte)(hex >> 24);
            byte g = (byte)(hex >> 16);
            byte b = (byte)(hex >> 8);
            return (a, JetRgbaColor.FromRgb(r, g, b));
        }

        private static float? GetArgumentAsFloatConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            float min, float max)
        {
            var expression = GetNamedArgument(arguments, parameterName)?.Expression;
            return ArgumentAsFloatConstant(min, max, expression);
        }

        /// <summary>
        ///     Validates and clamps float constants to a valid range.
        /// </summary>
        private static float? ArgumentAsFloatConstant(float min, float max, IExpression? expression)
        {
            if (expression == null) return null;

            double? value = null;
            if (expression.ConstantValue.IsDouble())
                value = expression.ConstantValue.DoubleValue;
            else if (expression.ConstantValue.IsFloat())
                value = expression.ConstantValue.FloatValue;
            else if (expression.ConstantValue.IsInteger())
                value = expression.ConstantValue.IntValue;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == null || value.Value.IsNanOrInf() || value.Value.Clamp(min, max) != value.Value)
                return null;

            return (float) value.Value;
        }
        
        /// <summary>
        ///     Extracts a byte constant value from a specific argument in a collection of arguments. The value is
        ///     clamped within a specific range (min, max).
        /// </summary>
        private static byte? GetArgumentAsByteConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            byte min, byte max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            
            // Checks if the value is an integer and within valid byte range (0 to 255) then casts it to a byte
            return constantValue != null && constantValue.IsInteger(out var value) && value >= min && value <= max
                ? (byte)value
                : null;
        }
        
        /// <summary>
        ///     Extracts a ushort constant value from a specific argument in a collection of arguments. The value is
        ///     clamped within a specific range (min, max).
        /// </summary>
        private static ushort? GetArgumentAsUshortConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            ushort min, ushort max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            
            // Checks if the value is an integer and within valid ushort range (0 to 65,535) then casts it to a ushort
            return constantValue != null && constantValue.IsInteger(out var value) && value.Clamp(min, max) == value
                ? (ushort)value
                : null;
        }
        
        /// <summary>
        ///     Extracts a uint constant value from a specific argument in a collection of arguments. The value is
        ///     clamped within a specific range (min, max). Used for byte4 type.
        /// </summary>
        private static uint? GetByte4ArgumentAsUintConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            uint min, uint max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            if (constantValue == null || !constantValue.IsUinteger()) return null;
            var value = (uint)constantValue.Value;
            return value >= min && value <= max ? value : null;
        }
        
        /// <summary>
        ///     Extracts a uint constant value from a specific argument in a collection of arguments. The value is
        ///     clamped within a specific range (min, max). Used for byte4 type.
        /// </summary>
        private static uint? GetByte4StringArgumentAsUintConstant(IEnumerable<ICSharpArgument> arguments,
            string parameterName, uint min, uint max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            if (constantValue == null) return null;
            
            // Check if argument is a valid hex string first
            if (!constantValue.IsString(out var hexString)) 
                return null;
            
            // Validate that hexString is exactly 10 characters long (0x + 10 hex digits) and that the first two
            // characters are "0x".
            if (hexString.Length != 10 || !hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return null;

            // Convert hex substring to uint
            if (!uint.TryParse(hexString?.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                return null;
            
            return hexValue >= min && hexValue <= max ? hexValue : null;
        }
        
        /// <summary>
        ///     Extracts a uint constant value from a specific argument in a collection of arguments. The value is
        ///     clamped within a specific range (min, max). Used for byte3 type.
        /// </summary>
        private static uint? GetByte3ArgumentAsUintConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            uint min, uint max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            return constantValue != null && constantValue.IsInteger(out var value) && value >= min && value <= max
                ? (uint)value
                : null;
        }
        
        /// <summary>
        ///     Extracts a uint constant value from a specific argument in a collection of arguments. The value is
        ///     clamped within a specific range (min, max). Used for byte3 type.
        /// </summary>
        private static uint? GetByte3StringArgumentAsUintConstant(IEnumerable<ICSharpArgument> arguments,
            string parameterName, uint min, uint max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            if (constantValue == null) return null;
            
            // Check if argument is a valid hex string first
            if (!constantValue.IsString(out var hexString)) 
                return null;
            
            // Validate that hexString is exactly 8 characters long (0x + 8 hex digits) and that the first two
            // characters are "0x".
            if (hexString.Length != 8 || !hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return null;

            // Convert hex substring to uint
            if (!uint.TryParse(hexString?.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                return null;
            
            return hexValue >= min && hexValue <= max ? hexValue : null;
        }

        /// <summary>
        ///     Finds an argument by its parameter name (eg. `r`, `g`, `b`, `a`).
        /// </summary>
        private static ICSharpArgument? GetNamedArgument(IEnumerable<ICSharpArgument> arguments, string parameterName)
        {
            return arguments.FirstOrDefault(a =>
                parameterName.Equals(a.MatchingParameter?.Element.ShortName, StringComparison.Ordinal));
        }
    }
}
