namespace LC1.Lab7
{
    public enum BinaryOp
    {
        Add,
        Sub,
        Mul,
        Div
    }

    public sealed class ProgramAst
    {
        public List<ConstDeclarationAst> Declarations { get; } = new();
    }

    public sealed class ConstDeclarationAst
    {
        public required string Name { get; init; }
        public required string TypeName { get; init; }
        public required ExprAst Initializer { get; init; }
    }

    public abstract class ExprAst { }

    public sealed class NumberLiteralAst : ExprAst
    {
        public required string SourceText { get; init; }
        public bool IsIntegerLiteral { get; init; }
    }

    public sealed class UnaryExprAst : ExprAst
    {
        public required ExprAst Operand { get; init; }
    }

    public sealed class BinaryExprAst : ExprAst
    {
        public required BinaryOp Op { get; init; }
        public required ExprAst Left { get; init; }
        public required ExprAst Right { get; init; }
    }
}
