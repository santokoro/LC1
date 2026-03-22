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
                    {
                        AddError($"Неожиданный токен: {Current?.Lexeme}");
                    }

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
                if (!panicMode)
                {
                    AddError("Ожидалось 'const'");
                    panicMode = true;
                }

                SkipTo(TokenKind.StatementEnd);

                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                return null;
            }

            decl.Children.Add(new AstNode { NodeType = "Модификатор", Token = Current });
            Next();

            
            if (!CheckKeyword("val"))
            {
                if (!panicMode)
                {
                    AddError("После 'const' ожидается 'val'");
                    panicMode = true;
                }

                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "КлючевоеСлово", Token = Current });
            Next();

            
            if (!Check(TokenKind.Identifier))
            {
                if (!panicMode)
                {
                    AddError("Ожидается имя переменной");
                    panicMode = true;
                }

                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "Идентификатор", Token = Current });
            Next();

            
            typeError = false;
            var type = ParseType();

            if (typeError)
            {
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                decl.Children.Add(type ?? new AstNode { NodeType = "Тип(ошибка)" });
                return decl;
            }

            if (type == null)
            {
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                return decl;
            }

            decl.Children.Add(type);

            
            if (!Check(TokenKind.Assignment))
            {
                if (!panicMode)
                {
                    AddError("Ожидается '='");
                    panicMode = true;
                }

                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                return decl;
            }

            decl.Children.Add(new AstNode { NodeType = "Оператор", Token = Current });
            Next();

            
            var expr = ParseExpression();
            if (expr == null)
            {
                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

                return decl;
            }

            decl.Children.Add(expr);

            // ';'
            if (!Check(TokenKind.StatementEnd))
            {
                if (!panicMode)
                {
                    AddError("Ожидается ';'");
                    panicMode = true;
                }

                SkipTo(TokenKind.StatementEnd);
                if (Check(TokenKind.StatementEnd))
                {
                    Next();
                    panicMode = false;
                }

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
                if (!panicMode)
                {
                    AddError("Ожидается ':' перед типом");
                    panicMode = true;
                }

                typeError = true;
                return null;
            }

            typeNode.Children.Add(new AstNode { NodeType = "Двоеточие", Token = Current });
            Next();

            if (!CheckKeyword("Double"))
            {
                if (!panicMode)
                {
                    AddError($"Ожидается тип Double, найдено '{Current?.Lexeme}'");
                    panicMode = true;
                }

                typeError = true;
                return null;
            }

            typeNode.Children.Add(new AstNode { NodeType = "ИмяТипа", Token = Current });
            Next();

            if (CheckKeyword("Double"))
            {
                if (!panicMode)
                {
                    AddError($"Лишний токен '{Current.Lexeme}' после типа");
                    panicMode = true;
                }

                typeError = true;
                return typeNode;
            }

            return typeNode;
        }

        private AstNode ParseExpression()
        {
            if (Check(TokenKind.Minus))
            {
                var minusToken = Current;
                Next();

                var number = ParseNumber();
                if (number == null)
                {
                    if (!panicMode)
                    {
                        AddError("После '-' ожидается число");
                        panicMode = true;
                    }

                    return null;
                }

                var node = new AstNode { NodeType = "УнарныйМинус" };
                node.Children.Add(new AstNode { NodeType = "Оператор", Token = minusToken });
                node.Children.Add(number);
                return node;
            }

            var num = ParseNumber();
            if (num == null)
            {
                if (!panicMode)
                {
                    AddError("Ожидается число");
                    panicMode = true;
                }

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
