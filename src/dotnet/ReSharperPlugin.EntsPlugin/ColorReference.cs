using System;
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.EntsPlugin
{
    /// <summary>
    /// Responsible for handling and manipulating color references in a codebase, such as RGB(A) constructor
    /// expressions.
    /// </summary>
    public class ColorReference : IColorReference
    {
        // Stores the owning expression (eg. constructor call, method invocation, or property access) that represents
        // the color.
        private readonly IExpression myOwningExpression;
        private IColorReference colorReferenceImplementation;

        public ColorReference(IColorElement colorElement, IExpression owningExpression, ITreeNode owner,
            DocumentRange colorConstantRange)
        {
            myOwningExpression = owningExpression;
            ColorElement = colorElement;
            Owner = owner;
            ColorConstantRange = colorConstantRange;

            // Defines binding options for the color reference (values only)
            BindOptions = new ColorBindOptions
            {
                BindsToValue = true
            };
        }

        /// <summary>
        ///     Attempts to replace the color reference with a new one using the provided `colorElement`.
        /// </summary>
        public void Bind(IColorElement colorElement)
        {
            // Replaces the reference with a constructor-based color (eg. `Color(r, g, b, a)`)
            TryReplaceAsConstructor(colorElement);
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return colorReferenceImplementation.GetColorTable();
        }

        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }
        public ColorBindOptions BindOptions { get; }

        /// <summary>
        ///     Replaces the current color reference with a constructor-based expression.
        /// </summary>
        private void TryReplaceAsConstructor(IColorElement colorElement)
        {
            // Extracts the RGB(A) values from the provided `colorElement`
            var newColor = colorElement.RGBColor;
            var colorType = GetColorType();
            if (colorType == null) return;

            var elementFactory = CSharpElementFactory.GetInstance(Owner);
            var module = myOwningExpression.GetPsiModule();
            var colorTypes = ColorTypes.GetInstance(module);

            var requiresAlpha = newColor.A != byte.MaxValue;

            // Handles both float and byte for RGB(A) components, depending on the type
            // (`ColorFloatType` vs. `ColorByteType`).
            ConstantValue r, g, b, a;
            if (colorTypes.ColorFloatType != null && colorTypes.ColorFloatType.Equals(colorType))
            {
                // Round to 2 decimal places, to match the values shown in the colour palette quick fix
                r = ConstantValue.Float((float) Math.Round(newColor.R / 255.0, 2), module);
                g = ConstantValue.Float((float) Math.Round(newColor.G / 255.0, 2), module);
                b = ConstantValue.Float((float) Math.Round(newColor.B / 255.0, 2), module);
                a = ConstantValue.Float((float) Math.Round(newColor.A / 255.0, 2), module);
            }
            else if (colorTypes.ColorByteType != null && colorTypes.ColorByteType.Equals(colorType))
            {
                // ReSharper formats byte constants with an explicit cast
                r = ConstantValue.Byte(newColor.R, module);
                g = ConstantValue.Byte(newColor.G, module);
                b = ConstantValue.Byte(newColor.B, module);
                a = ConstantValue.Byte(newColor.A, module);
                requiresAlpha = true;
            }
            else
                return;

            ICSharpExpression newExp;
            if (!requiresAlpha)
            {
                newExp = elementFactory
                    .CreateExpression("new $0($1, $2, $3)", colorType,
                        elementFactory.CreateExpressionByConstantValue(r),
                        elementFactory.CreateExpressionByConstantValue(g),
                        elementFactory.CreateExpressionByConstantValue(b));
            }
            else
            {
                newExp = elementFactory
                    .CreateExpression("new $0($1, $2, $3, $4)", colorType,
                        elementFactory.CreateExpressionByConstantValue(r),
                        elementFactory.CreateExpressionByConstantValue(g),
                        elementFactory.CreateExpressionByConstantValue(b),
                        elementFactory.CreateExpressionByConstantValue(a));
            }

            var oldExp = (ICSharpExpression) myOwningExpression;
            oldExp.ReplaceBy(newExp);
        }

        /// <summary>
        ///     Determines the color type based on the owning expression.
        /// </summary>
        private ITypeElement GetColorType()
        {
            // Checks if the expression is a method call (eg. float4.Rgba())
            if (myOwningExpression is IInvocationExpression invocationExpression)
                return invocationExpression.Reference?.Resolve().DeclaredElement as ITypeElement;

            // Checks if the expression is an object creation (eg. new float4())
            var objectCreationExpression = myOwningExpression as IObjectCreationExpression;
            return objectCreationExpression?.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        }
    }
}