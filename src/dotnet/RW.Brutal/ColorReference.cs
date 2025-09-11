using System.Collections.Generic;
using System.Globalization;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Media;

namespace RW.Brutal
{
    /// <summary>
    ///     Responsible for handling and manipulating color references in a codebase, such as RGB(A) constructor
    ///     expressions.
    /// </summary>
    public class ColorReference : IColorReference
    {
        // Stores the owning expression (eg. constructor call, method invocation, or property access) that represents
        // the color.
        private readonly IExpression myOwningExpression;

        public ColorReference(IColorElement colorElement, IExpression owningExpression, ITreeNode owner,
            DocumentRange colorConstantRange)
        {
            myOwningExpression = owningExpression;
            ColorElement = colorElement;
            Owner = owner;
            ColorConstantRange = colorConstantRange;

            // Defines binding options for the color reference
            BindOptions = new ColorBindOptions
            {
                BindsToName = true,
                BindsToValue = true
            };
        }
        
        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }
        public ColorBindOptions BindOptions { get; }
        public bool CanSetColor => true;

        /// <summary>
        ///     Attempts to replace the color reference with a new one using the provided `colorElement`.
        /// </summary>
        public void Bind(IColorElement colorElement)
        {
            if (!TryReplaceAsNamedColor(colorElement))
                TryReplaceAsNumericLiteral(colorElement.RGBColor);
        }
        
        public void SetColor(JetRgbaColor newColor)
        {
            TryReplaceAsNumericLiteral(newColor);
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return BrutalNamedColors.GetColorTable();
        }
        
        /// <summary>
        ///     Attempts to replace the color with a named color using the current expression.
        /// </summary>
        private bool TryReplaceAsNamedColor(IColorElement colorElement)
        {
            var colorType = GetColorType();

            // Checks if the named color is compatible with the type and generates a new expression like `Color.Red`
            var newColor = ColorTypes.PropertyFromColorElement(colorType, colorElement,
                myOwningExpression.GetPsiModule());
            if (newColor == null) return false;

            var newExp = CSharpElementFactory.GetInstance(Owner)
                .CreateExpression("$0.$1", newColor.Value.First, newColor.Value.Second);

            var oldExp = myOwningExpression as ICSharpExpression;
            return oldExp?.ReplaceBy(newExp) != null;
        }
        
        /// <summary>
        ///     Correctly updates existing float3, float4, ushort3, ushort4 invocation with new values when using the
        ///     Color Picker (eg. float4.Rgb(1, 0, 0)). Preserves the method name and argument count.
        /// </summary>
        private void TryReplaceAsNumericLiteral(JetRgbaColor color)
        {
            if (myOwningExpression is not IInvocationExpression invocation)
                return;

            var args = invocation.ArgumentList.Arguments;
            var factory = CSharpElementFactory.GetInstance(invocation);
            var psiModule = myOwningExpression.GetPsiModule();
            var colorTypes = ColorTypes.GetInstance(psiModule);
            
            // Resolve the method
            var resolvedElement = invocation.Reference?.Resolve().DeclaredElement as IMethod;
            if (resolvedElement == null)
                return;
            
            // Check the return type
            var returnType = resolvedElement.ReturnType as IDeclaredType;
            var returnTypeElement = returnType?.GetTypeElement();
            if (returnTypeElement == null)
                return;
            
            // Safe to proceed
            if (returnTypeElement.Equals(colorTypes.ColorFloat3Type) ||
                returnTypeElement.Equals(colorTypes.ColorFloat4Type))
            {
                ReplaceFloat(0, color.R / 255f);
                ReplaceFloat(1, color.G / 255f);
                ReplaceFloat(2, color.B / 255f);
                if (args.Count >= 4)
                    ReplaceFloat(3, color.A / 255f);
            }
            else if (returnTypeElement.Equals(colorTypes.ColorUshort3Type) ||
                     returnTypeElement.Equals(colorTypes.ColorUshort4Type))
            {
                ReplaceUshort(0, color.R);
                ReplaceUshort(1, color.G);
                ReplaceUshort(2, color.B);
                if (args.Count >= 4)
                    ReplaceUshort(3, color.A);
            }

            void ReplaceFloat(int index, float value)
            {
                if (index >= args.Count || args[index]?.Value == null) return;

                var expr = factory.CreateExpression(
                    value.ToString("0.###", CultureInfo.InvariantCulture));
                LowLevelModificationUtil.ReplaceChild(args[index].Value, expr);
            }

            void ReplaceUshort(int index, byte component)
            {
                if (index >= args.Count || args[index]?.Value == null) return;

                // Scale from byte (0-255) to ushort (0-65535)
                ushort ushortValue = (ushort)(component / 255.0 * 65535.0);
                string hex = "0x" + ushortValue.ToString("X4"); // e.g., 0xFFFF

                var expr = factory.CreateExpression(hex);
                LowLevelModificationUtil.ReplaceChild(args[index].Value, expr);
            }
        }

        /// <summary>
        ///     Determines the color type based on the owning expression.
        /// </summary>
        private ITypeElement GetColorType()
        {
            // Checks if the expression is a method call (eg. float4.Rgba())
            if (myOwningExpression is IInvocationExpression invocationExpression) 
                return invocationExpression.Reference?.Resolve().DeclaredElement as ITypeElement;
            return null;
        }
    }
}