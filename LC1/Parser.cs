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

        private bool typeError = false;
        private bool panicMode = false;

        private Token Current => position < tokens.Count ? tokens[position] : null;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens.Where(t => t.Code != (int)TokenKind.Space).ToList();
        }

        private void SkipTo(params TokenKind[] kinds)
        {
            while (Current != null && !kinds.Contains((TokenKind)Current.Code))
                Next();
        }

        private int FindNext(TokenKind kind)
        {
            for (int i = position; i < tokens.Count; i++)
                if (tokens[i].Code == (int)kind)
                    return i;
            return -1;
        }

        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };

            while (Current != null)
            {
                var decl = ParseDeclaration();

                if (decl != null)
                {
                    program.Children.Add(decl);
                }
                else
                {
                    if (!panicMode)
                        AddError($"Неожиданный токен: {Current?.Lexeme}");

                    Next();
                }
            }

            return program;
        }

        private AstNode ParseDeclaration()
        {
            var decl = new AstNode { NodeType = "Объявление" };

            
            if (!CheckKeyword("const"))
            {
                AddError("Ожидалось 'const'");
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return null;
            }

            decl.Children.Add(new AstNode { NodeType = "Модификатор", Token = Current });
            Next();

            
            if (!CheckKeyword("val"))
            {
                AddError("После 'const' ожидается 'val'");
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "КлючевоеСлово", Token = Current });
            Next();

            
            if (!Check(TokenKind.Identifier))
            {
                AddError("Ожидается имя переменной");
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "Идентификатор", Token = Current });
            Next();

            
            typeError = false;
            var type = ParseType();

            if (typeError)
            {
                
                int semi = FindNext(TokenKind.StatementEnd);
                if (semi == -1)
                {
                    AddError("Ожидается ';'");
                    SkipTo(TokenKind.StatementEnd);
                    if (Check(TokenKind.StatementEnd)) Next();
                }
                else
                {
                    var before = tokens[Math.Max(0, semi - 1)];
                    errors.Add(new SyntaxError
                    {
                        Line = before.Line,
                        Column = before.End + 1,
                        Message = "Ожидается ';'",
                        Token = before
                    });

                    position = semi;
                    Next();
                }

                decl.Children.Add(type ?? new AstNode { NodeType = "Тип(ошибка)" });
                return decl;
            }

            if (type == null)
            {
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return decl;
            }

            decl.Children.Add(type);

            
            if (!Check(TokenKind.Assignment))
            {
                AddError("Ожидается '='");
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "Оператор", Token = Current });
            Next();

           
            var expr = ParseExpression();
            if (expr == null)
            {
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return decl;
            }

            decl.Children.Add(expr);

            
            if (!Check(TokenKind.StatementEnd))
            {
                AddError("Ожидается ';'");
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd)) Next();
                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "Разделитель", Token = Current });
            Next();

            return decl;
        }

        private AstNode ParseType()
        {
            var typeNode = new AstNode { NodeType = "Тип" };

            if (!Check(TokenKind.Colon))
            {
                AddError("Ожидается ':' перед типом");
                typeError = true;
                return typeNode;
            }

            typeNode.Children.Add(new AstNode { NodeType = "Двоеточие", Token = Current });
            Next();

            if (!CheckKeyword("Double"))
            {
                AddError($"Ожидается тип Double, найдено '{Current?.Lexeme}'");
                typeError = true;
                return typeNode;
            }

            typeNode.Children.Add(new AstNode { NodeType = "ИмяТипа", Token = Current });
            Next();

            return typeNode;
        }

        private AstNode ParseExpression()
        {
            if (Check(TokenKind.Minus))
            {
                var minus = Current;
                Next();

                var number = ParseNumber();
                if (number == null)
                {
                    AddError("После '-' ожидается число");
                    return null;
                }

                var node = new AstNode { NodeType = "УнарныйМинус" };
                node.Children.Add(new AstNode { NodeType = "Оператор", Token = minus });
                node.Children.Add(number);
                return node;
            }

            var num = ParseNumber();
            if (num == null)
            {
                AddError("Ожидается число");
                return null;
            }

            return num;
        }

        private AstNode ParseNumber()
        {
            if (Check(TokenKind.RealNumber) || Check(TokenKind.UnsignedInteger))
            {
                var node = new AstNode { NodeType = "Число", Token = Current };
                Next();
                return node;
            }

            return null;
        }

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
