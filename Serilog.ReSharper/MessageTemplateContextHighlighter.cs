using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextHighlighters;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Special;

namespace Serilog.ReSharper
{
    [ContainsContextConsumer]
    public class MessageTemplateContextHighlighter : ContextHighlighterBase
    {
        [AsyncContextConsumer]
        public static Action ProcessContext(Lifetime lifetime, [ContextKey(typeof(CSharpContextActionDataProvider.ContextKey))] ICSharpContextActionDataProvider dataProvider, InvisibleBraceHintManager invisibleBraceHintManager, MatchingBraceSuggester matchingBraceSuggester)
        {
            return new MessageTemplateContextHighlighter().ProcessDataContextImpl(lifetime, dataProvider, invisibleBraceHintManager, matchingBraceSuggester);
        }

        protected override void CollectHighlightings(IContextActionDataProvider dataProvider, MatchingHighlightingsConsumer consumer)
        {
            var literalExpression = dataProvider.GetSelectedElement<IExpression>(true, true);
            if (literalExpression == null)
            {
                return;
            }

            var stringConcat = StringConcatenationClrWrapperUtil.CreateWidestContainingStringConcatenation(literalExpression);
            if (stringConcat == null ||
                !stringConcat.Expression.ConstantValue.IsString() ||
                !FunctionInvocation.IsArgumentOfLoggingMethod(stringConcat.Expression))
            {
                return;
            }

            var literalByExpression = StringLiteralAltererUtil.TryCreateStringLiteralByExpression(literalExpression);
            if (literalByExpression == null)
            {
                return;
            }

            var constantArguments = stringConcat
                .GetWidestSequencesOfConstantArguments()
                .ToList();

            var matchingArguments = constantArguments
                .SingleOrDefault(x => x.Contains(literalExpression))
                .IfNotNull(x => x.ToList());

            if (matchingArguments == null)
            {
                return;
            }

            var formatString = matchingArguments.AggregateString((sb, expr) => sb.Append(expr.ConstantValue.Value));

            var formatItems = FormatStringParser.Parse(formatString).ToList();

            var formatItem = GetFormatItem(
                matchingArguments,
                formatItems,
                dataProvider,
                literalByExpression,
                literalExpression);

            if (formatItem == null)
            {
                return;
            }

            foreach (var list in constantArguments)
            {
                var documentRanges = StringLiteralAltererUtil
                    .GetDocumentRangesByInnerRangeInExpressionSequence(list, formatItem.Range, false);

                foreach (var range in documentRanges)
                {
                    consumer.ConsumeHighlighting("ReSharper Matched Format String Item", range);
                }
            }

            var matchedFormatItemIndex = formatItems.IndexOf(formatItem);

            HighlightArgument(consumer, stringConcat, matchedFormatItemIndex);
        }

        private static void HighlightArgument(
            MatchingHighlightingsConsumer consumer,
            IStringConcatenationClrWrapper stringConcat,
            int formatItemIndex)
        {
            var functionInvocation = FunctionInvocationUtil
                .GetFunctionInvocationByArgument(stringConcat.Expression);

            if (functionInvocation == null)
            {
                return;
            }

            var templateIndex = functionInvocation.Arguments.IndexOf(stringConcat.Expression);

            var index = templateIndex + formatItemIndex + 1;

            var expression = functionInvocation.Arguments.ElementAtOrDefault(index);
            if (expression == null)
            {
                return;
            }

            var documentRange = expression.GetDocumentRange();

            consumer.ConsumeHighlighting("ReSharper Matched Format String Item", documentRange);
        }

        private static FormatStringParser.FormatItem GetFormatItem(
            IList<IExpression> matchingArguments,
            IList<FormatStringParser.FormatItem> formatItems,
            IContextActionDataProvider dataProvider,
            IStringLiteralAlterer literalByExpression,
            IExpression literalExpression)
        {
            var valueOffset = literalByExpression.PresentationOffsetToValueOffset(dataProvider.CaretOffset);

            var innerOffset = valueOffset + matchingArguments
                .Take(matchingArguments.IndexOf(literalExpression))
                .Aggregate(0, (acc, le) => acc + ((string) le.ConstantValue.Value).Length);

            var isAtEndOfLiteral = valueOffset == ((string) literalByExpression.Expression.ConstantValue.Value).Length;

            var formatItem = formatItems.FirstOrDefault(x => x.Range.TrimLeft(isAtEndOfLiteral ? 1 : 0).TrimRight(1).Contains(innerOffset));

            if (formatItem == null && valueOffset != 0)
            {
                formatItem = formatItems.FirstOrDefault(x => x.Range.EndOffset == innerOffset);
            }

            return formatItem;
        }
    }
}