using System;
using System.Collections.Generic;
using System.Linq;

namespace LC1
{
    public class SyntaxError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
        public Token Token { get; set; }
    }

    public class AstNode
    {
        public string NodeType { get; set; }
        public Token Token { get; set; }
        public List<AstNode> Children { get; set; } = new();
    }

    public class Parser
    {
        private readonly List<Token> tokens;
        private int pos = 0;
        private readonly List<SyntaxError> errors = new();

        private Token Current =>
            pos < tokens.Count ? tokens[pos] : new Token
            {
                Code = -1,
                Lexeme = "",
                Line = tokens.Count > 0 ? tokens[^1].Line : 1,
                Start = tokens.Count > 0 ? tokens[^1].End + 1 : 1,
                End = tokens.Count > 0 ? tokens[^1].End + 1 : 1
            };

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens
                .Where(t => t.Code != (int)TokenKind.Space)
                .ToList();
        }

        private bool AtEnd() => pos >= tokens.Count;

        private void Next()
        {
            if (pos < tokens.Count)
                pos++;
        }

        private void AddError(string msg)
        {
            errors.Add(new SyntaxError
            {
                Line = Current.Line,
                Column = Current.Start,
                Message = msg,
                Token = Current
            });
        }

        private void SkipTo(params int[] codes)
        {
            while (!AtEnd())
            {
                int c = Current.Code;
                foreach (var x in codes)
                    if (c == x) return;
                Next();
            }
        }

        private bool ExpectKeyword(string lexeme, string msg, params int[] follow)
        {
            if (!AtEnd() &&
                Current.Code == (int)TokenKind.Keyword &&
                Current.Lexeme == lexeme)
            {
                Next();
                return true;
            }

            if (AtEnd())
            {
                AddError(msg);
                return false;
            }

            int startPos = pos;
            AddError(msg);

            SkipTo((int)TokenKind.Keyword, (int)TokenKind.Identifier,
                   (int)TokenKind.Colon, (int)TokenKind.StatementEnd);

            if (!AtEnd() &&
                Current.Code == (int)TokenKind.Keyword &&
                Current.Lexeme == lexeme)
            {
                Next();
                return true;
            }

            if (AtEnd() || follow.Contains(Current.Code))
                return false;

            if (pos == startPos && !AtEnd())
                Next();

            return false;
        }

        private bool Expect(int code, string msg, params int[] follow)
        {
            if (!AtEnd() && Current.Code == code)
            {
                Next();
                return true;
            }

            if (AtEnd())
            {
                AddError(msg);
                return false;
            }

            int startPos = pos;
            AddError(msg);

            var sync = new List<int> { code };
            sync.AddRange(follow);

            SkipTo(sync.ToArray());

            if (!AtEnd() && Current.Code == code)
            {
                Next();
                return true;
            }

            if (AtEnd() || follow.Contains(Current.Code))
                return false;

            if (pos == startPos && !AtEnd())
                Next();

            return false;
        }

        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };

            if (AtEnd())
                return program;

            program.Children.Add(ParseDeclaration());

            if (!AtEnd())
                AddError("Неожиданный текст после объявления");

            return program;
        }

        private AstNode ParseDeclaration()
        {
            var decl = new AstNode { NodeType = "Объявление" };

            // const
            ExpectKeyword("const", "Ожидалось 'const'",
                (int)TokenKind.Keyword, (int)TokenKind.Identifier,
                (int)TokenKind.Colon, (int)TokenKind.StatementEnd);

            // val
            ExpectKeyword("val", "После 'const' ожидается 'val'",
                (int)TokenKind.Identifier, (int)TokenKind.Colon,
                (int)TokenKind.StatementEnd);

            // id
            Expect((int)TokenKind.Identifier, "Ожидается имя переменной",
                (int)TokenKind.Colon, (int)TokenKind.StatementEnd);

            // :
            Expect((int)TokenKind.Colon, "Ожидается ':' перед типом",
                (int)TokenKind.Keyword, (int)TokenKind.StatementEnd);

            // Double
            ExpectKeyword("Double", "Ожидается тип Double",
                (int)TokenKind.Assignment, (int)TokenKind.StatementEnd);

            // =
            Expect((int)TokenKind.Assignment, "Ожидается '='",
                (int)TokenKind.RealNumber, (int)TokenKind.UnsignedInteger,
                (int)TokenKind.StatementEnd);

            // число
            if (!Expect((int)TokenKind.RealNumber, "Ожидается число",
                (int)TokenKind.StatementEnd))
            {
                Expect((int)TokenKind.UnsignedInteger, "Ожидается число",
                    (int)TokenKind.StatementEnd);
            }

            // ;
            Expect((int)TokenKind.StatementEnd, "Ожидается ';'");

            return decl;
        }

        public List<SyntaxError> GetErrors() => errors;

        public string PrintAst(AstNode node, string indent = "")
        {
            if (node == null) return "";

            string result = indent + "└─ " + node.NodeType;

            if (node.Token != null)
                result += $" ({node.Token.Lexeme})";

            result += "\n";

            foreach (var child in node.Children)
                result += PrintAst(child, indent + "   ");

            return result;
        }
    }
}
