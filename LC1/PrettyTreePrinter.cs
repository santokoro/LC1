using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Text;

namespace LC1
{
    internal static class PrettyTreePrinter
    {
        public static string Print(IParseTree tree, KotlinConstParser parser)
        {
            var sb = new StringBuilder();
            PrintNode(tree, parser, sb, "", true);
            return sb.ToString();
        }

        private static void PrintNode(IParseTree node, KotlinConstParser parser, StringBuilder sb, string indent, bool last)
        {
            sb.Append(indent);
            sb.Append(last ? " └── " : " ├── ");

            string text = node is ParserRuleContext ctx
                ? parser.RuleNames[ctx.RuleIndex]
                : node.GetText();

            sb.AppendLine(text);

            indent += last ? "     " : " │   ";

            for (int i = 0; i < node.ChildCount; i++)
            {
                PrintNode(node.GetChild(i), parser, sb, indent, i == node.ChildCount - 1);
            }
        }
    }
}
