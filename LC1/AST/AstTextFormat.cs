using System.Text;

namespace LC1.Ast
{
    public static class AstTextFormat
    {
        public static string FormatProgram(ProgramNode program)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ProgramNode");
            if (program.Declarations.Count == 0)
            {
                sb.AppendLine("    (объявлений нет)");
                return sb.ToString().TrimEnd();
            }

            for (int d = 0; d < program.Declarations.Count; d++)
            {
                bool lastDecl = d == program.Declarations.Count - 1;
                FormatConstDecl(sb, program.Declarations[d], "", lastDecl);
            }

            return sb.ToString().TrimEnd();
        }

        private static void FormatConstDecl(StringBuilder sb, ConstDeclNode n, string indent, bool last)
        {
            string p = last ? "└── " : "├── ";
            sb.Append(indent).Append(p).AppendLine("ConstDeclNode");
            string c = indent + (last ? "    " : "│   ");

            sb.Append(c).AppendLine("├── KeywordModifierNode");
            sb.Append(c).Append("│   ").AppendLine("└── keyword: \"const\"");
            sb.Append(c).AppendLine("├── KeywordModifierNode");
            sb.Append(c).Append("│   ").AppendLine("└── keyword: \"val\"");
            sb.Append(c).AppendLine("├── IdentifierNode");
            sb.Append(c).Append("│   ").AppendLine("└── name: \"" + Escape(n.Identifier.Name) + "\"");
            sb.Append(c).AppendLine("├── DoubleTypeNode");
            sb.Append(c).Append("│   ").AppendLine("└── name: \"" + Escape(n.Type.TypeName) + "\"");
            sb.Append(c).AppendLine("└── DoubleLiteralNode");
            sb.Append(c).Append("    ").AppendLine("└── value: " + FormatDouble(n.Value.Value) + " (текст: \"" + Escape(n.Value.RawText) + "\")");
        }

        private static string FormatDouble(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                return v.ToString();
            return v.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
