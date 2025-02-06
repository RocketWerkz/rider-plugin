using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains;
using JetBrains.Application.Settings;
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
        
        private static IColorReference? GetColorReference(ITreeNode element, IEnumerable<IColorReferenceProvider> providers)
        {
            // Checks if the node is a `new` expression (eg. `new Color(r, g, b)`)
            if (element is IObjectCreationExpression constructorExpression)
                return ReferenceFromConstructor(constructorExpression);

            foreach (var provider in providers)
            {
                var result = provider.GetColorReference(element);
                if (result != null)
                    return result;
            }
            
            return null;
        }

        /// <summary>
        ///     Handles color references created via constructors (eg. `new Color(r, g, b, a)`).
        /// </summary>
        private static IColorReference? ReferenceFromConstructor(IObjectCreationExpression constructorExpression)
        {
            // Get the type from the constructor, which allows us to support target typed `new`. This will fail to
            // resolve if the parameters don't match (eg. calling new Color(r, g, b) without passing `a`), so fall back
            // to the expression's type if available.
            // Note that we don't do further validation of the parameters, so we'll still show a colour preview for
            // Color(r, g, b) even though it's an invalid method call.
            var constructedType =
                (constructorExpression.ConstructorReference.Resolve().DeclaredElement as IConstructor)?.ContainingType
                ?? constructorExpression.TypeReference?.Resolve().DeclaredElement as ITypeElement;
            if (constructedType == null)
                return null;

            var colorTypes = ColorTypes.GetInstance(constructedType.Module);
            if (!colorTypes.IsColorType(constructedType)) return null;

            var arguments = constructorExpression.Arguments;
            if (arguments.Count is < 3 or > 4) return null;

            // Checks if the type matches known color type `Color`
            JetRgbaColor? color = null;
            if (colorTypes.ColorType != null && colorTypes.ColorType.Equals(constructedType))
            {
                var baseColor = GetColorFromFloatArgb(arguments);
                if (baseColor == null) return null;

                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)(255.0 * a)) : rgb;
            }

            if (color == null) return null;

            var colorElement = new ColorElement(color.Value);
            var argumentList = constructorExpression.ArgumentList;
            return new ColorReference(colorElement, constructorExpression, argumentList, argumentList.GetDocumentRange());
        }

        /// <summary>
        ///     Extracts ARGB values from float arguments (eg. `new Color(0.5f, 0.2f, 0.8f)`).
        /// </summary>
        private static (float? alpha, JetRgbaColor)? GetColorFromFloatArgb(ICollection<ICSharpArgument> arguments)
        {
            var a = GetArgumentAsFloatConstant(arguments, "a", 0, 1);
            var r = GetArgumentAsFloatConstant(arguments, "r", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "g", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "b", 0, 1);

            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)(255.0 * r.Value), (byte)(255.0 * g.Value), (byte)(255.0 * b.Value)));
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
        ///     Finds an argument by its parameter name (eg. `r`, `g`, `b`, `a`).
        /// </summary>
        private static ICSharpArgument? GetNamedArgument(IEnumerable<ICSharpArgument> arguments, string parameterName)
        {
            return arguments.FirstOrDefault(a =>
                parameterName.Equals(a.MatchingParameter?.Element.ShortName, StringComparison.Ordinal));
        }
    }
}
