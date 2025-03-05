using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Tree;

namespace RW.Brutal
{
    static class CompletionExtensions
    {
        public static ICSharpLiteralExpression StringLiteral(this CSharpCodeCompletionContext context)
            => context.NodeInFile is ITokenNode nodeInFile
               && nodeInFile.Parent is ICSharpLiteralExpression literalExpression
               && literalExpression.Literal.IsAnyStringLiteral()
                ? literalExpression
                : null;

        public static IClrTypeName InvokedMethodFirstTypeArgument(this IInvocationExpression invocation)
        {
            var typeArgs = invocation.Reference.Invocation.TypeArguments;
            return typeArgs.Count == 1
                   && typeArgs[0] is IDeclaredType t
                ? t.GetClrName()
                : null;
        }

        public static IClrTypeName AssignmentDestType(this IInvocationExpression invocation)
            => AssignmentExpressionNavigator.GetBySource(invocation) is IAssignmentExpression assignment 
               && assignment.Dest.Type() is IDeclaredType lhsType
                ? lhsType.GetClrName()
                : null;

        public static IClrTypeName GetResourceType(this CSharpCodeCompletionContext context)
        {
            if (!(
                    InvocationExpressionNavigator.GetByArgument(
                            CSharpArgumentNavigator.GetByValue(
                                context.NodeInFile.Parent as ICSharpLiteralExpression))
                        is IInvocationExpression invocation))
            {
                return null;
            }

            return invocation.InvokedMethodFirstTypeArgument()
                   ?? invocation.AssignmentDestType();
        }
    }
}