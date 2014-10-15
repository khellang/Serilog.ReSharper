using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace Serilog.ReSharper
{
    internal static class FunctionInvocation
    {
        public static bool IsArgumentOfLoggingMethod(IExpression expression)
        {
            return GetMethods(expression.GetParameters()).Any(IsLoggingMethod);
        }

        private static IEnumerable<IMethod> GetMethods(IEnumerable<DeclaredElementInstance<IParameter>> parameters)
        {
            return parameters.Select(x => x.Element.ContainingParametersOwner).OfType<IMethod>();
        }

        private static bool IsLoggingMethod(this IParametersOwner method)
        {
            return HasMessageTemplateParameter(method) && HasParamsArrayParameter(method);
        }

        private static bool HasParamsArrayParameter(this IParametersOwner method)
        {
            return method.Parameters.Any(IsPropertyValuesArray);
        }

        private static bool IsPropertyValuesArray(this IParameter parameter)
        {
            return parameter.IsParameterArray && parameter.HasName("propertyValues");
        }

        private static bool HasMessageTemplateParameter(this IParametersOwner method)
        {
            return method.Parameters.Any(IsMessageTemplateParameter);
        }

        private static bool IsMessageTemplateParameter(this ITypeOwner parameter)
        {
            return parameter.Type.IsString() && parameter.HasName("messageTemplate");
        }

        private static bool HasName(this IDeclaredElement parameter, string parameterName)
        {
            return parameter.ShortName.Equals(parameterName, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<DeclaredElementInstance<IParameter>> GetParameters(this ITreeNode expression)
        {
            var parameters = expression.GetParameters<ICSharpArgument>(
                InvocationExpressionNavigator.GetByArgument,
                (argument, invocation) => argument)
                .ToList();

            if (parameters.Any())
            {
                return parameters;
            }

            return expression.GetParameters<IReferenceExpression>(
                InvocationExpressionNavigator.GetByInvokedExpression,
                (reference, invocation) => invocation.ExtensionQualifier)
                .ToList();
        }

        private static IEnumerable<DeclaredElementInstance<IParameter>> GetParameters<T>(
            this ITreeNode treeNode,
            Func<T, IInvocationExpression> invocationFactory,
            Func<T, IInvocationExpression, ICSharpArgumentInfo> argumentSelector) where T : class
        {
            var parent = treeNode.Parent as T;
            if (parent == null)
            {
                yield break;
            }

            var invocation = invocationFactory.Invoke(parent);
            if (invocation == null)
            {
                yield break;
            }

            var argumentInfo = argumentSelector.Invoke(parent, invocation);

            var parameters = ArgumentsUtil.GetPossibleParameters(argumentInfo);

            foreach (var parameter in parameters)
            {
                yield return parameter;
            }
        }
    }
}