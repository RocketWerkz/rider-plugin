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
    /// processing). Highlights color-related expressions (eg. `new Color(r, g, b)`) in the editor.
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
        ///     Attempts to retrieve a color reference and ensures only one reference is returned (so there are no two
        ///     color icon displays).
        /// </summary>
        private static IColorReference? GetColorReference(ITreeNode element, IEnumerable<IColorReferenceProvider> providers)
        {
            // Checks if a `new` object creation expression (eg. `new float4(r, g, b, a)`)
            if (element is IObjectCreationExpression constructorExpression)
            {
                var reference = ReferenceFromConstructor(constructorExpression);
                if (reference != null)
                    return reference;
            }
            
            // Checks if an invocation expression (eg. `float4.Rgba(r, g, b, a)`)
            if (element is IReferenceExpression { QualifierExpression: IReferenceExpression qualifier } referenceExpression)
            {
                var reference = ReferenceFromInvocation(qualifier, referenceExpression);
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
        
        // We can remove this method once we confirm `ReferenceFromInvocation` works as we don't want to support float4
        // color structs.
        /// <summary>
        ///     Handles color references created via constructors.
        ///     eg. `new float4(r, g, b, a)`.
        ///     eg. `new byte4(r, g, b, a)`.
        /// </summary>
        private static IColorReference? ReferenceFromConstructor(IObjectCreationExpression constructorExpression)
        {
            // Get the type from the constructor, which allows us to support target typed `new`. This will fail to
            // resolve if the parameters don't match (eg. calling new float4(r, g, b) without passing `a`), so fall
            // back to the expression's type if available.
            // Note that we don't do further validation of the parameters, so we'll still show a colour preview for
            // float4(r, g, b) even though it's an invalid method call.
            var constructedType =
                (constructorExpression.ConstructorReference.Resolve().DeclaredElement as IConstructor)?.ContainingType
                ?? constructorExpression.TypeReference?.Resolve().DeclaredElement as ITypeElement;
            if (constructedType == null)
                return null;

            var colorTypes = ColorTypes.GetInstance(constructedType.Module);
            if (!colorTypes.IsColorType(constructedType)) return null;

            var arguments = constructorExpression.Arguments;
            if (arguments.Count is < 3 or > 4) return null;

            // Checks if the type matches known color types (`ColorFloatType`, `ColorByteType`)
            JetRgbaColor? color = null;
            if (colorTypes.ColorFloatType != null && colorTypes.ColorFloatType.Equals(constructedType))
            {
                // Unwind the float4 argument values into a string for logging purposes
                var argValues = string.Join(", ", arguments.Select(arg => arg.Value?.GetText()));
                Log.Root.Error($"ReferenceFromConstructor ColorFloatType argValues: {argValues}");

                // Attempt to parse color from floating-point RGBA
                var baseColor = GetColorFromFloatXyzw(arguments);
                if (baseColor == null) return null;

                // If an alpha value exists, adjust the color's transparency, otherwise default to full opacity (255)
                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)(255.0 * a)) : rgb;
            }
            else if (colorTypes.ColorByteType != null && colorTypes.ColorByteType.Equals(constructedType))
            {
                // Unwind the byte4 argument values into a string for logging purposes
                var argValues = string.Join(", ", arguments.Select(arg => arg.Value?.GetText()));
                Log.Root.Error($"ReferenceFromConstructor ColorByteType argValues: {argValues}");

                var baseColor = GetColorFromByteXyzw(arguments);
                if (baseColor == null) return null;
                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)a) : rgb;
            }

            if (color == null) return null;

            var colorElement = new ColorElement(color.Value);
            var argumentList = constructorExpression.ArgumentList;
            return new ColorReference(colorElement, constructorExpression, argumentList, argumentList.GetDocumentRange());
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
            if (colorTypes.ColorFloatType != null && colorTypes.ColorFloatType.Equals(qualifierType))
            {
                // Attempt to parse color from floating-point RGBA
                var baseColor = GetColorFromFloatRgba(arguments);
                if (baseColor == null) return null;
                
                // If an alpha value exists, adjust the color's transparency, otherwise default to full opacity (255)
                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)(255.0 * a)) : rgb;
            }
            else if (colorTypes.ColorByteType != null && colorTypes.ColorByteType.Equals(qualifierType))
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
        ///     Extracts RGBA values from float arguments via xyzw (eg. `new float4(0.5f, 0.2f, 0.8f, 1f)`).
        /// </summary>
        private static (float? alpha, JetRgbaColor)? GetColorFromFloatXyzw(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsFloatConstant(arguments, "x", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "y", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "z", 0, 1);
            var a = GetArgumentAsFloatConstant(arguments, "w", 0, 1);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)(255.0 * r.Value), (byte)(255.0 * g.Value), (byte)(255.0 * b.Value)));
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
        ///     Extracts RGBA values from byte arguments via xyzw (eg. `new byte4(255, 0, 0, 255)`).
        /// </summary>
        private static (int? alpha, JetRgbaColor)? GetColorFromByteXyzw(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsIntConstant(arguments, "x", 0, 255);
            var g = GetArgumentAsIntConstant(arguments, "y", 0, 255);
            var b = GetArgumentAsIntConstant(arguments, "z", 0, 255);
            var a = GetArgumentAsIntConstant(arguments, "w", 0, 255);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;
            return (a, JetRgbaColor.FromRgb((byte)r.Value, (byte)g.Value, (byte)b.Value));
        }
        
        /// <summary>
        ///     Extracts RGBA values from byte arguments via rgba (eg. `byte4.Rgba(r, g, b, a)`).
        /// </summary>
        private static (int? alpha, JetRgbaColor)? GetColorFromByteRgba(ICollection<ICSharpArgument> arguments)
        {
            var r = GetArgumentAsIntConstant(arguments, "r", 0, 255);
            var g = GetArgumentAsIntConstant(arguments, "g", 0, 255);
            var b = GetArgumentAsIntConstant(arguments, "b", 0, 255);
            var a = GetArgumentAsIntConstant(arguments, "a", 0, 255);
            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;
            return (a, JetRgbaColor.FromRgb((byte)r.Value, (byte)g.Value, (byte)b.Value));
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
        
        private static int? GetArgumentAsIntConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            int min, int max)
        {
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            return constantValue != null && constantValue.IsInteger(out var value) && value.Clamp(min, max) == value
                ? value
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
