using System;
using System.Collections.Generic;
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

namespace ReSharperPlugin.EntsPlugin
{
    /// <summary>
    /// `CSharpIncrementalDaemonStageProcessBase` is a base class for analyzing C# code in small chunks (incremental
    /// processing). Highlights color-related expressions (eg. `float4.Rgba(r, g, b, a`) in the editor.
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

            var colorInfo = CreateColorHighlightingInfo(element);
            
            // If a valid color is found, add a highlight to the editor
            if (colorInfo != null)
                consumer.AddHighlighting(colorInfo.Highlighting, colorInfo.Range);
        }

        private HighlightingInfo? CreateColorHighlightingInfo(ITreeNode element)
        {
            var colorReference = GetColorReference(element);
            var range = colorReference?.ColorConstantRange;
            return range?.IsValid() == true
                ? new HighlightingInfo(range.Value, new ColorHintHighlighting(colorReference))
                : null;
        }
        
        /// <summary>
        ///     Attempts to retrieve a color reference and ensures only one reference is returned (so there are no
        ///     multiple color icon displays).
        /// </summary>
        private static IColorReference? GetColorReference(ITreeNode element)
        {
            // Checks if an invocation expression (eg. `float4.Rgba(r, g, b, a)` or `Color.red`)
            if (element is not IReferenceExpression 
                {
                    QualifierExpression: IReferenceExpression qualifier
                } referenceExpression) return null;
            
            var reference = ReferenceFromInvocation(qualifier, referenceExpression);
            if (reference != null)
                return reference;
                
            reference = ReferenceFromProperty(qualifier, referenceExpression);
            if (reference != null)
                return reference;

            return null;
        }
        
        /// <summary>
        ///     Handles color references created via an invocation of the `float4.Rgba(r, g, b, a)` method.
        ///     Or `byte4.Rgba(r, g, b, a)`.
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
            
            // The method name must be hardcoded so temporarily use `Rgba` until confirmed
            if (!string.Equals(name, "Rgba", StringComparison.Ordinal)) return null;
            
            var arguments = invocationExpression.Arguments;
            if (arguments.Count is < 3 or > 4) return null;
            
            // Unwind the argument values into a string for logging purposes
            var argValues = string.Join(", ", arguments.Select(arg => arg.Value?.GetText()));
            Log.Root.Error($"argValues: {argValues}");
            
            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;

            var colorTypes = ColorTypes.GetInstance(qualifierType.Module);
            if (!colorTypes.IsColorType(qualifierType)) return null;
            
            // Checks if the type matches known color types (`ColorFloatType`, `ColorByteType`)
            JetRgbaColor? color = null;
            if (colorTypes.ColorFloat4Type != null && colorTypes.ColorFloat4Type.Equals(qualifierType))
            {
                // Attempt to parse color from floating-point RGBA
                var baseColor = GetColorFromFloatRgba(arguments);
                if (baseColor == null) return null;
                
                // If an alpha value exists, adjust the color's transparency, otherwise default to full opacity (255)
                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)(255.0 * a)) : rgb;
            }
            else if (colorTypes.ColorByte4Type != null && colorTypes.ColorByte4Type.Equals(qualifierType))
            {
                var baseColor = GetColorFromByteRgba(arguments);
                if (baseColor == null) return null;
                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)a) : rgb;
            }
            
            if (color == null) return null;
            
            var colorElement = new ColorElement(color.Value);
            var argumentList = invocationExpression.ArgumentList;
            return new ColorReference(colorElement, invocationExpression, argumentList, argumentList.GetDocumentRange());
        }
        
        /// <summary>
        ///     Handles color references created from a predefined color property (eg. `Color.red`).
        /// </summary>
        private static IColorReference? ReferenceFromProperty(IReferenceExpression qualifier,
            IReferenceExpression colorQualifiedMemberExpression)
        {
            // Get the name of the referenced property (eg., "red" in "Color.red")
            var name = colorQualifiedMemberExpression.Reference.GetName();

            // Look up the name in our predefined color list
            var color = BrutalNamedColors.Get(name);
            if (color == null) return null;

            // Resolve the type of the qualifier (eg., "Color" in "Color.red") -> in our case should be float4 or byte4?
            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;
            
            var colorTypes = ColorTypes.GetInstance(qualifierType.Module);
            if (!colorTypes.IsColorTypeSupportingProperties(qualifierType)) return null;

            var property = colorQualifiedMemberExpression.Reference.Resolve().DeclaredElement as IProperty;
            if (property == null) return null;

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

        private static float? GetArgumentAsFloatConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            float min, float max)
        {
            var expression = GetNamedArgument(arguments, parameterName)?.Expression;
            return ArgumentAsFloatConstant(min, max, expression);
        }

        /// <summary>
        ///     Validates and clamps float constants to a valid range.
        /// </summary>
        public static float? ArgumentAsFloatConstant(float min, float max, IExpression? expression)
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
            
            // Checks if the value is an integer and within valid ushort range (0 to 255) then casts it to a ushort
            return constantValue != null && constantValue.IsInteger(out var value) && value >= min && value <= max
                ? (ushort)value
                : null;
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
