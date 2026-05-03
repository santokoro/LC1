using System.Collections.Generic;

namespace LC1.Ast
{
    public abstract class AstNode
    {
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }

        public abstract string NodeTypeName { get; }
        public abstract IReadOnlyList<AstNode> GetChildren();
    }

    public sealed class ProgramNode : AstNode
    {
        public List<ConstDeclNode> Declarations { get; } = new();

        public override string NodeTypeName => "ProgramNode";

        public override IReadOnlyList<AstNode> GetChildren() => Declarations;
    }

    public sealed class KeywordModifierNode : AstNode
    {
        public string Keyword { get; set; } = "";

        public override string NodeTypeName => "KeywordModifierNode";

        public override IReadOnlyList<AstNode> GetChildren() => System.Array.Empty<AstNode>();
    }

    public sealed class IdentifierNode : AstNode
    {
        public string Name { get; set; } = "";

        public override string NodeTypeName => "IdentifierNode";

        public override IReadOnlyList<AstNode> GetChildren() => System.Array.Empty<AstNode>();
    }

    public sealed class ConstDeclNode : AstNode
    {
        public KeywordModifierNode ConstKeyword { get; set; } = null!;
        public KeywordModifierNode ValKeyword { get; set; } = null!;
        public IdentifierNode Identifier { get; set; } = null!;
        public DoubleTypeNode Type { get; set; } = null!;
        public DoubleLiteralNode Value { get; set; } = null!;

        public string Name => Identifier.Name;

        public override string NodeTypeName => "ConstDeclNode";

        public override IReadOnlyList<AstNode> GetChildren() =>
            new AstNode[] { ConstKeyword, ValKeyword, Identifier, Type, Value };
    }

    public sealed class DoubleTypeNode : AstNode
    {
        public string TypeName { get; set; } = "Double";

        public override string NodeTypeName => "DoubleTypeNode";

        public override IReadOnlyList<AstNode> GetChildren() => System.Array.Empty<AstNode>();
    }

    public sealed class DoubleLiteralNode : AstNode
    {
        public double Value { get; set; }
        public string RawText { get; set; } = "";

        public override string NodeTypeName => "DoubleLiteralNode";

        public override IReadOnlyList<AstNode> GetChildren() => System.Array.Empty<AstNode>();
    }
}
