using System;
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

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

        /// <summary>
        ///     Attempts to replace the color reference with a new one using the provided `colorElement`.
        /// </summary>
        public void Bind(IColorElement colorElement)
        {
            TryReplaceAsNamedColor(colorElement);
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return BrutalNamedColors.GetColorTable();
        }

        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }
        public ColorBindOptions BindOptions { get; }
        
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