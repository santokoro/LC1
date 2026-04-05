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

        
        private bool errorHappened = false;

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
            
            if (!errorHappened)
            {
                errors.Add(new SyntaxError
                {
                    Line = Current.Line,
                    Column = Current.Start,
                    Message = msg,
                    Token = Current
                });
                errorHappened = true; 
            }
        }

        private bool ExpectKeyword(string lexeme, string msg, params int[] follow)
        {
            if (!AtEnd() && Current.Code == (int)TokenKind.Keyword && Current.Lexeme == lexeme)
            {
                Next();
                errorHappened = false; 
                return true;
            }

            AddError(msg);

            
            int tempPos = pos;
            while (tempPos < tokens.Count)
            {
                var t = tokens[tempPos];
                if (t.Code == (int)TokenKind.Keyword && t.Lexeme == lexeme)
                {
                    pos = tempPos + 1; 
                    errorHappened = false; 
                    return true;
                }
                if (t.Code == (int)TokenKind.StatementEnd) break;
                tempPos++;
            }

          
            tempPos = pos;
            while (tempPos < tokens.Count)
            {
                var t = tokens[tempPos];
                if (follow.Contains(t.Code) || t.Code == (int)TokenKind.StatementEnd)
                {
                    pos = tempPos;
                    return false;
                }
                tempPos++;
            }
            return false;
        }

        private bool Expect(int code, string msg, params int[] follow)
        {
            if (!AtEnd() && Current.Code == code)
            {
                Next();
                errorHappened = false; 
                return true;
            }

            AddError(msg);

           
            int tempPos = pos;
            while (tempPos < tokens.Count)
            {
                var t = tokens[tempPos];
                if (t.Code == code)
                {
                    pos = tempPos + 1;
                    errorHappened = false;
                    return true;
                }
                if (t.Code == (int)TokenKind.StatementEnd) break;
                tempPos++;
            }

      
            tempPos = pos;
            while (tempPos < tokens.Count)
            {
                var t = tokens[tempPos];
                if (follow.Contains(t.Code) || t.Code == (int)TokenKind.StatementEnd)
                {
                    pos = tempPos;
                    return false;
                }
                tempPos++;
            }
            return false;
        }

        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };
            if (AtEnd()) return program;

            program.Children.Add(ParseDeclaration());

            if (!AtEnd() && !errorHappened) 
                AddError("Неожиданный текст после объявления");

            return program;
        }

        private AstNode ParseDeclaration()
        {
            var decl = new AstNode { NodeType = "Объявление" };
            errorHappened = false; 

            ExpectKeyword("const", "Ожидалось 'const'", (int)TokenKind.Keyword, (int)TokenKind.Identifier);
            ExpectKeyword("val", "После 'const' ожидается 'val'", (int)TokenKind.Identifier, (int)TokenKind.Colon);
            Expect((int)TokenKind.Identifier, "Ожидается имя переменной", (int)TokenKind.Colon);
            Expect((int)TokenKind.Colon, "Ожидается ':' перед типом", (int)TokenKind.Keyword);
            ExpectKeyword("Double", "Ожидается тип Double", (int)TokenKind.Assignment);
            Expect((int)TokenKind.Assignment, "Ожидается '='", (int)TokenKind.RealNumber, (int)TokenKind.UnsignedInteger);

           
            if (!Expect((int)TokenKind.RealNumber, "Ожидается число", (int)TokenKind.StatementEnd))
            {
                Expect((int)TokenKind.UnsignedInteger, "Ожидается число", (int)TokenKind.StatementEnd);
            }

            Expect((int)TokenKind.StatementEnd, "Ожидается ';'");

            return decl;
        }

        public List<SyntaxError> GetErrors() => errors;

        public string PrintAst(AstNode node, string indent = "")
        {
            if (node == null) return "";
            string result = indent + "└─ " + node.NodeType;
            if (node.Token != null) result += $" ({node.Token.Lexeme})";
            result += "\n";
            foreach (var child in node.Children)
                result += PrintAst(child, indent + "   ");
            return result;
        }
    }
}