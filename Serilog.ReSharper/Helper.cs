using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextHighlighters;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace Serilog.ReSharper
{
    public class Helper
    {
        public void CollectHighlightings(IContextActionDataProvider dataProvider, MatchingHighlightingsConsumer consumer)
        {
            var literalExpression = dataProvider
                .GetSelectedElement<IExpression>(true, true);
            if (literalExpression == null)
            {
                return;
            }

            var argumentExpression = FunctionInvocationUtil
                .GetNarrowestContainingArgumentExpression(literalExpression);
            if (argumentExpression == null)
            {
                return;
            }

            var functionInvocation = FunctionInvocationUtil
                .GetFunctionInvocationByArgument(argumentExpression);
            if (functionInvocation == null)
            {
                return;
            }

            var candidates = functionInvocation.InvokedFunctionCandidates;
            if (candidates.IsEmpty())
            {
                return;
            }
        }
    }
}