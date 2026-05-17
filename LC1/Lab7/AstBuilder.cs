namespace LC1.Lab7
{
    public static class AstBuilder
    {
        public static ProgramAst Build(KotlinConstParser.ProgramContext program)
        {
            var ast = new ProgramAst();
            foreach (var decl in program.declaration())
                ast.Declarations.Add(BuildDeclaration(decl));
            return ast;
        }

        private static ConstDeclarationAst BuildDeclaration(KotlinConstParser.DeclarationContext ctx) =>
            new()
            {
                Name = ctx.IDENT()!.GetText(),
                TypeName = ctx.DOUBLE()!.GetText(),
                Initializer = BuildExpr(ctx.expr())
            };

        private static ExprAst BuildExpr(KotlinConstParser.ExprContext ctx) => ctx switch
        {
            KotlinConstParser.AddSubContext add => new BinaryExprAst
            {
                Op = add.MINUS() != null ? BinaryOp.Sub : BinaryOp.Add,
                Left = BuildExpr(add.expr()),
                Right = BuildTerm(add.term())
            },
            KotlinConstParser.ExprTermContext term => BuildTerm(term.term()),
            _ => throw new InvalidOperationException("Неизвестный узел expr")
        };

        private static ExprAst BuildTerm(KotlinConstParser.TermContext ctx) => ctx switch
        {
            KotlinConstParser.MulDivContext mul => new BinaryExprAst
            {
                Op = mul.STAR() != null ? BinaryOp.Mul : BinaryOp.Div,
                Left = BuildTerm(mul.term()),
                Right = BuildFactor(mul.factor())
            },
            KotlinConstParser.TermFactorContext factor => BuildFactor(factor.factor()),
            _ => throw new InvalidOperationException("Неизвестный узел term")
        };

        private static ExprAst BuildFactor(KotlinConstParser.FactorContext ctx) => ctx switch
        {
            KotlinConstParser.ParenContext paren => BuildExpr(paren.expr()),
            KotlinConstParser.UnaryMinusContext unary => new UnaryExprAst
            {
                Operand = BuildFactor(unary.factor())
            },
            KotlinConstParser.FactorNumberContext num => BuildNumber(num.number()),
            _ => throw new InvalidOperationException("Неизвестный узел factor")
        };

        private static NumberLiteralAst BuildNumber(KotlinConstParser.NumberContext ctx)
        {
            bool isInt = ctx.INT() != null;
            string text = isInt ? ctx.INT()!.GetText() : ctx.REAL()!.GetText();
            return new NumberLiteralAst
            {
                SourceText = text,
                IsIntegerLiteral = isInt
            };
        }
    }
}
