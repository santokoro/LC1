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
        private int position = 0;
        private readonly List<SyntaxError> errors = new();

        private Token Current => position < tokens.Count ? tokens[position] : null;

        // --- КОНТЕКСТНЫЕ ФЛАГИ ---
        private bool afterConst = false;
        private bool inType = false;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens
                .Where(t => t.Code != (int)TokenKind.Space)
                .ToList();
        }

        private void Next()
        {
            if (position < tokens.Count)
                position++;
        }

        private bool Check(TokenKind kind) =>
            Current != null && Current.Code == (int)kind;

        private bool CheckKeyword(string keyword) =>
            Current != null &&
            Current.Code == (int)TokenKind.Keyword &&
            Current.Lexeme == keyword;

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

        private Token Fake(TokenKind kind, string lexeme)
        {
            return new Token
            {
                Code = (int)kind,
                Lexeme = lexeme,
                Line = Current?.Line ?? 0,
                Start = Current?.Start ?? 0,
                End = Current?.Start ?? 0
            };
        }

        // ============================
        // МУСОРНЫЕ ТОКЕНЫ (контекстные)
        // ============================
        private bool IsGarbage(Token t)
        {
            if (t == null) return false;

            // 1) Error-токены — всегда мусор
            if (t.Code == (int)TokenKind.Error)
                return true;

            // 2) неизвестные ключевые слова — мусор
            if (t.Code == (int)TokenKind.Keyword &&
                t.Lexeme != "const" &&
                t.Lexeme != "val" &&
                t.Lexeme != "Double")
                return true;

            // 3) после const допускается только val
            if (afterConst)
            {
                if (!(t.Code == (int)TokenKind.Keyword && t.Lexeme == "val"))
                    return true;
            }

            // 4) внутри типа допускается только Double
            if (inType)
            {
                if (!(t.Code == (int)TokenKind.Keyword && t.Lexeme == "Double"))
                    return true;
            }

            return false;
        }

        private void SkipGarbage()
        {
            while (IsGarbage(Current))
            {
                AddError($"Неожиданный токен '{Current.Lexeme}'");
                Next();
            }
        }

        private bool ExistsAhead(Func<Token, bool> pred)
        {
            int i = position;
            while (i < tokens.Count && tokens[i].Code != (int)TokenKind.StatementEnd)
            {
                if (pred(tokens[i]))
                    return true;
                i++;
            }
            return false;
        }

        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };

            while (Current != null)
            {
                int startPos = position;

                program.Children.Add(ParseDeclaration());

                if (position == startPos)
                    Next();
            }

            return program;
        }

        private AstNode ParseDeclaration()
        {
            var decl = new AstNode { NodeType = "Объявление" };

            SkipGarbage();

            // const
            afterConst = false;
            if (!CheckKeyword("const"))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.Keyword && t.Lexeme == "const"))
                    AddError("Ожидалось 'const'");

                decl.Children.Add(new AstNode { NodeType = "(восстановлено const)", Token = Fake(TokenKind.Keyword, "const") });
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = "const", Token = Current });
                Next();
            }

            // теперь ждём val
            afterConst = true;

            SkipGarbage();

            // val
            if (!CheckKeyword("val"))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.Keyword && t.Lexeme == "val"))
                    AddError("После 'const' ожидается 'val'");

                decl.Children.Add(new AstNode { NodeType = "(восстановлено val)", Token = Fake(TokenKind.Keyword, "val") });
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = "val", Token = Current });
                Next();
            }

            afterConst = false;

            SkipGarbage();

            // identifier
            if (!Check(TokenKind.Identifier))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.Identifier))
                    AddError("Ожидается имя переменной");

                decl.Children.Add(new AstNode { NodeType = "(восстановлено id)", Token = Fake(TokenKind.Identifier, "id") });
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = "Идентификатор", Token = Current });
                Next();
            }

            SkipGarbage();

            // type
            decl.Children.Add(ParseType());

            SkipGarbage();

            // =
            if (!Check(TokenKind.Assignment))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.Assignment))
                    AddError("Ожидается '='");

                decl.Children.Add(new AstNode { NodeType = "(восстановлено '=')", Token = Fake(TokenKind.Assignment, "=") });
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = "=", Token = Current });
                Next();
            }

            SkipGarbage();

            // expression
            decl.Children.Add(ParseExpression());

            SkipGarbage();

            // ;
            if (!Check(TokenKind.StatementEnd))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.StatementEnd))
                    AddError("Ожидается ';'");

                decl.Children.Add(new AstNode { NodeType = "(восстановлено ';')", Token = Fake(TokenKind.StatementEnd, ";") });
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = ";", Token = Current });
                Next();
            }

            return decl;
        }

        private AstNode ParseType()
        {
            var typeNode = new AstNode { NodeType = "Тип" };

            SkipGarbage();

            // :
            if (!Check(TokenKind.Colon))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.Colon))
                    AddError("Ожидается ':' перед типом");

                typeNode.Children.Add(new AstNode { NodeType = "(восстановлено ':')", Token = Fake(TokenKind.Colon, ":") });
            }
            else
            {
                typeNode.Children.Add(new AstNode { NodeType = ":", Token = Current });
                Next();
            }

            SkipGarbage();

            // теперь ждём Double
            inType = true;

            // Double
            if (!CheckKeyword("Double"))
            {
                if (!ExistsAhead(t => t.Code == (int)TokenKind.Keyword && t.Lexeme == "Double"))
                    AddError("Ожидается тип Double");

                typeNode.Children.Add(new AstNode { NodeType = "(восстановлено Double)", Token = Fake(TokenKind.Keyword, "Double") });
            }
            else
            {
                typeNode.Children.Add(new AstNode { NodeType = "Double", Token = Current });
                Next();
            }

            inType = false;

            return typeNode;
        }

        private AstNode ParseExpression()
        {
            SkipGarbage();

            if (Check(TokenKind.Minus))
            {
                var minus = Current;
                Next();

                SkipGarbage();

                var number = ParseNumber();
                if (number == null)
                {
                    AddError("После '-' ожидается число");
                    return new AstNode { NodeType = "(восстановлено число)", Token = Fake(TokenKind.RealNumber, "0") };
                }

                return new AstNode
                {
                    NodeType = "УнарныйМинус",
                    Children =
                    {
                        new AstNode { NodeType = "-", Token = minus },
                        number
                    }
                };
            }

            var num = ParseNumber();
            if (num == null)
            {
                if (!ExistsAhead(t =>
                    t.Code == (int)TokenKind.RealNumber ||
                    t.Code == (int)TokenKind.UnsignedInteger))
                {
                    AddError("Ожидается число");
                }

                return new AstNode { NodeType = "(восстановлено число)", Token = Fake(TokenKind.RealNumber, "0") };
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
