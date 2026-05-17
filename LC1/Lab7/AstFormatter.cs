using System.Text;

namespace LC1.Lab7
{
    public static class AstFormatter
    {
        public static string Format(ProgramAst program)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Program");
            for (int i = 0; i < program.Declarations.Count; i++)
                FormatDeclaration(program.Declarations[i], sb, "", i == program.Declarations.Count - 1);
            return sb.ToString().TrimEnd();
        }

        private static void FormatDeclaration(ConstDeclarationAst decl, StringBuilder sb, string indent, bool last)
        {
            sb.Append(indent).Append(last ? " └── " : " ├── ");
            sb.AppendLine("ConstDeclaration");
            string childIndent = indent + (last ? "     " : " │   ");
            AppendLeaf(sb, childIndent, "const", false);
            AppendLeaf(sb, childIndent, "val", false);
            AppendLeaf(sb, childIndent, $"Ident({decl.Name})", false);
            AppendLeaf(sb, childIndent, ":", false);
            AppendLeaf(sb, childIndent, $"Type({decl.TypeName})", false);
            AppendLeaf(sb, childIndent, "=", false);
            FormatExpr(decl.Initializer, sb, childIndent, true);
            AppendLeaf(sb, childIndent, ";", true);
        }

        private static void FormatExpr(ExprAst expr, StringBuilder sb, string indent, bool last)
        {
            sb.Append(indent).Append(last ? " └── " : " ├── ");
            switch (expr)
            {
                case NumberLiteralAst n:
                    sb.AppendLine($"NumberLiteral({n.SourceText})");
                    break;
                case UnaryExprAst u:
                    sb.AppendLine("UnaryMinus");
                    FormatExpr(u.Operand, sb, indent + "     ", true);
                    break;
                case BinaryExprAst b:
                    sb.AppendLine($"Binary({OpName(b.Op)})");
                    string ci = indent + "     ";
                    FormatExpr(b.Left, sb, ci, false);
                    FormatExpr(b.Right, sb, ci, true);
                    break;
            }
        }

        private static string OpName(BinaryOp op) => op switch
        {
            BinaryOp.Add => "+",
            BinaryOp.Sub => "-",
            BinaryOp.Mul => "*",
            BinaryOp.Div => "/",
            _ => "?"
        };

        private static void AppendLeaf(StringBuilder sb, string indent, string text, bool last)
        {
            sb.Append(indent).Append(last ? " └── " : " ├── ").AppendLine(text);
        }
    }
}
