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
        public List<AstNode> Children { get; set; } = new List<AstNode>();
    }

    public class Parser
    {
        private readonly List<Token> tokens;
        private int position = 0;
        private readonly List<SyntaxError> errors = new();

        private Token Current => position < tokens.Count ? tokens[position] : null;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens.Where(t => t.Code != (int)TokenKind.Space).ToList();
        }

        // ------------------------------------------------------------
        // <Программа> → { <Объявление> }
        // ------------------------------------------------------------
        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };

            while (Current != null)
            {
                int before = position;
                var decl = ParseDeclaration();

                if (decl != null)
                {
                    program.Children.Add(decl);
                }
                else
                {
                    position = before;
                    AddError($"Неожиданный токен: {Current?.Lexeme}");
                    Next();
                }
            }

            return program;
        }

        // ------------------------------------------------------------
        // <Объявление> → ['const'] 'val' IDENT [':' TYPE] '=' <Выражение> ';'
        // ------------------------------------------------------------
        private AstNode ParseDeclaration()
        {
            int start = position;

            if (Current == null) return null;

            var decl = new AstNode { NodeType = "Объявление" };

            // const
            if (CheckKeyword("const"))
            {
                decl.Children.Add(new AstNode { NodeType = "Модификатор", Token = Current });
                Next();

                if (!CheckKeyword("val"))
                {
                    AddError("После 'const' ожидается 'val'");
                    position = start;
                    return null;
                }
            }

            // val
            if (!CheckKeyword("val"))
            {
                if (decl.Children.Count == 0)
                    return null;

                position = start;
                return null;
            }

            decl.Children.Add(new AstNode { NodeType = "КлючевоеСлово", Token = Current });
            Next();

            // идентификатор
            if (!Check(TokenKind.Identifier))
            {
                AddError("Ожидается имя переменной");
                position = start;
                return null;
            }

            decl.Children.Add(new AstNode { NodeType = "Идентификатор", Token = Current });
            Next();

            // тип
            if (Check(TokenKind.Colon))
            {
                var type = ParseType();
                if (type == null)
                {
                    position = start;
                    return null;
                }
                decl.Children.Add(type);
            }

            // =
            if (!Check(TokenKind.Assignment))
            {
                AddError("Ожидается '='");
                position = start;
                return null;
            }

            decl.Children.Add(new AstNode { NodeType = "Оператор", Token = Current });
            Next();

            // выражение
            var expr = ParseExpression();
            if (expr == null)
            {
                position = start;
                return null;
            }

            decl.Children.Add(expr);

            // ;
            if (!Check(TokenKind.StatementEnd))
            {
                AddError("Ожидается ';'");
                position = start;
                return null;
            }

            decl.Children.Add(new AstNode { NodeType = "Разделитель", Token = Current });
            Next();

            return decl;
        }

        // ------------------------------------------------------------
        // <Тип> → ':' ('Double' | 'Float')
        // ------------------------------------------------------------
        private AstNode ParseType()
        {
            var typeNode = new AstNode { NodeType = "Тип" };

            typeNode.Children.Add(new AstNode { NodeType = "Двоеточие", Token = Current });
            Next();

            if (!CheckKeyword("Double") && !CheckKeyword("Float"))
            {
                AddError("Ожидается Double или Float");
                return null;
            }

            typeNode.Children.Add(new AstNode { NodeType = "ИмяТипа", Token = Current });
            Next();

            return typeNode;
        }

        // ------------------------------------------------------------
        // ВЫРАЖЕНИЯ
        // <Expression> → <Factor> { ('*' | '/') <Factor> }
        // ------------------------------------------------------------
        private AstNode ParseExpression()
        {
            var left = ParseFactor();
            if (left == null) return null;

            while (Check(TokenKind.Multiply) || Check(TokenKind.Divide))
            {
                var op = Current;
                Next();

                var right = ParseFactor();
                if (right == null)
                {
                    AddError("Ожидается выражение после оператора");
                    return null;
                }

                var node = new AstNode { NodeType = "Операция" };
                node.Children.Add(left);
                node.Children.Add(new AstNode { NodeType = "Оператор", Token = op });
                node.Children.Add(right);

                left = node;
            }

            return left;
        }

        private AstNode ParseFactor()
        {
            if (Check(TokenKind.RealNumber) || Check(TokenKind.UnsignedInteger))
            {
                var node = new AstNode { NodeType = "Число", Token = Current };
                Next();
                return node;
            }

            if (Check(TokenKind.Identifier))
            {
                var node = new AstNode { NodeType = "Идентификатор", Token = Current };
                Next();
                return node;
            }

            AddError("Ожидается число или идентификатор");
            return null;
        }

        // ------------------------------------------------------------
        // ВСПОМОГАТЕЛЬНЫЕ
        // ------------------------------------------------------------
        private bool Check(TokenKind kind) =>
            Current != null && Current.Code == (int)kind;

        private bool CheckKeyword(string keyword) =>
            Current != null &&
            Current.Code == (int)TokenKind.Keyword &&
            Current.Lexeme == keyword;

        private void Next() => position++;

        private void AddError(string message)
        {
            errors.Add(new SyntaxError
            {
                Line = Current?.Line ?? 0,
                Column = Current?.Start ?? 0,
                Message = message,
                Token = Current
            });
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
