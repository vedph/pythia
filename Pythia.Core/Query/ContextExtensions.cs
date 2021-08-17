using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;

namespace Pythia.Core.Query
{
    /// <summary>
    /// Extensions for <see cref="ParserRuleContext"/>.
    /// See https://stackoverflow.com/questions/28457534/antlr4-get-left-and-right-sibling-of-rule-context.
    /// </summary>
    public static class ContextExtensions
    {
        /// <summary>
        /// Returns the left sibling of the parse tree node.
        /// </summary>
        /// <param name="context">A node.</param>
        /// <returns>Left sibling of a node, or null if no sibling is found.</returns>
        public static IParseTree GetLeftSibling(this ParserRuleContext context)
        {
            int index = GetNodeIndex(context);

            return index > 0
                ? context.Parent.GetChild(index - 1)
                : null;
        }

        /// <summary>
        /// Returns the right sibling of the parse tree node.
        /// </summary>
        /// <param name="context">A node.</param>
        /// <returns>Right sibling of a node, or null if no sibling is found.</returns>
        public static IParseTree GetRightSibling(this ParserRuleContext context)
        {
            int index = GetNodeIndex(context);

            return index >= 0 && index < context.Parent.ChildCount - 1
                ? context.Parent.GetChild(index + 1)
                : null;
        }

        /// <summary>
        /// Gets the right siblings of the parse tree node.
        /// </summary>
        /// <param name="context">The node.</param>
        /// <returns>Right siblings.</returns>
        public static IEnumerable<IParseTree> GetRightSiblings(
            this ParserRuleContext context)
        {
            int index = GetNodeIndex(context);
            if (index == -1) yield break;

            index++;
            while (index < context.Parent.ChildCount)
            {
                yield return context.Parent.GetChild(index);
                index++;
            }
        }

        /// <summary>
        /// Returns the node's index with in its parent's children array.
        /// </summary>
        /// <param name="context">A child node.</param>
        /// <returns>Node's index or -1 if node is null or doesn't have a parent.
        /// </returns>
        public static int GetNodeIndex(this ParserRuleContext context)
        {
            RuleContext parent = context?.Parent;

            if (parent == null)
                return -1;

            for (int i = 0; i < parent.ChildCount; i++)
            {
                if (parent.GetChild(i) == context)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
