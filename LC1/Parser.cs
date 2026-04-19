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
        private bool errorReportedForCurrentConstruct = false;

        private Token Current => pos < tokens.Count
            ? tokens[pos]
            : new Token
            {
                Code = -1,
                Lexeme = "",
                Line = tokens.Count > 0 ? tokens[^1].Line : 1,
                Start = tokens.Count > 0 ? tokens[^1].End + 1 : 1,
                End = tokens.Count > 0 ? tokens[^1].End + 1 : 1
            };

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens.Where(t => t.Code != (int)TokenKind.Space).ToList();
        }

        private bool AtEnd() => pos >= tokens.Count;
        private void Next() { if (pos < tokens.Count) pos++; }

        private void ReportError(string msg)
        {
            if (!errorReportedForCurrentConstruct)
            {
                errors.Add(new SyntaxError
                {
                    Line = Current.Line,
                    Column = Current.Start,
                    Message = msg,
                    Token = Current
                });
                errorReportedForCurrentConstruct = true;
            }
        }

        private bool Expect(int code, string errorMessage, params int[] follow)
        {
            if (!AtEnd() && Current.Code == code)
            {
                Next();
                return true;
            }

            ReportError(errorMessage);

            int temp = pos;
            while (temp < tokens.Count)
            {
                if (tokens[temp].Code == code)
                {
                    pos = temp + 1;
                    errorReportedForCurrentConstruct = false;
                    return true;
                }
                if (tokens[temp].Code == (int)TokenKind.StatementEnd)
                    break;
                temp++;
            }
            return false;
        }

        private bool Synchronize(params int[] follow)
        {
            int temp = pos;
            while (temp < tokens.Count)
            {
                if (follow.Contains(tokens[temp].Code) || tokens[temp].Code == (int)TokenKind.StatementEnd)
                {
                    pos = temp;
                    return true;
                }
                temp++;
            }
            pos = temp;
            return false;
        }

        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };
            if (tokens.Count == 0)
            {
                var dummy = new Token { Code = -1, Lexeme = "", Line = 1, Start = 1, End = 1 };
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидалось 'const'", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "После 'const' ожидается 'val'", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидается имя переменной", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидается ':' перед типом", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидается тип Double", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидается '='", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидается число", Token = dummy });
                errors.Add(new SyntaxError { Line = 1, Column = 1, Message = "Ожидается ';' в конце оператора", Token = dummy });
                return program;
            }

            while (!AtEnd())
            {
                errorReportedForCurrentConstruct = false;
                var decl = ParseDeclaration();
                if (decl != null)
                    program.Children.Add(decl);

                if (!AtEnd() && (Current.Code != (int)TokenKind.Keyword || Current.Lexeme != "const"))
                {
                    while (!AtEnd() && (Current.Code != (int)TokenKind.Keyword || Current.Lexeme != "const"))
                    {
                        var t = Current;
                        if (t.Code == (int)TokenKind.StatementEnd)
                        {
                            errors.Add(new SyntaxError
                            {
                                Line = t.Line,
                                Column = t.Start,
                                Message = "Лишняя ';' после завершения объявления.",
                                Token = t
                            });
                        }
                        else if (t.Code != (int)TokenKind.Error)
                        {
                            errors.Add(new SyntaxError
                            {
                                Line = t.Line,
                                Column = t.Start,
                                Message = $"Неожиданный токен '{t.Lexeme}' после завершения объявления. Ожидалось 'const' или конец файла.",
                                Token = t
                            });
                        }
                        Next();
                    }
                }
            }
            return program;
        }

        private AstNode ParseDeclaration()
        {
            var decl = new AstNode { NodeType = "Объявление" };

            int startPos = pos;
            int endPos = pos;
            while (endPos < tokens.Count && tokens[endPos].Code != (int)TokenKind.StatementEnd)
                endPos++;
            bool suppressGarbageErrors = false;
            bool headerInvalid = false;

            void AddErrorAt(string msg, Token t)
            {
                
                if (t != null && t.Code == (int)TokenKind.Error)
                {
                    t = null;
                }

                errors.Add(new SyntaxError
                {
                    Line = t?.Line ?? (tokens.Count > 0 ? tokens[^1].Line : 1),
                    Column = t?.Start ?? (tokens.Count > 0 ? tokens[^1].End + 1 : 1),
                    Message = msg,
                    Token = t
                });
            }
            int FindTokenAndCheckGarbage(int from, int code, string lexeme, string expectedName, out bool hasGarbage)
            {
                hasGarbage = false;

                for (int i = from; i < endPos; i++)
                {
                    
                    if (tokens[i].Code == (int)TokenKind.Error)
                        continue;

                    if (tokens[i].Code == code && (lexeme == null || tokens[i].Lexeme == lexeme))
                    {
                        if (i > from)
                        {
                            hasGarbage = true;

                            if (!suppressGarbageErrors)
                            {
                                for (int j = from; j < i; j++)
                                {
                                    
                                    if (tokens[j].Code == (int)TokenKind.Error)
                                        continue;

                                    AddErrorAt(
                                        $"Неожиданный токен '{tokens[j].Lexeme}' перед ожидаемым '{expectedName}'",
                                        tokens[j]
                                    );
                                }
                            }
                        }
                        return i;
                    }
                }

                return -1;
            }






            bool garbage;
            int idxConst = FindTokenAndCheckGarbage(startPos, (int)TokenKind.Keyword, "const", "const", out garbage);
            if (idxConst == -1)
            {
                AddErrorAt("Ожидалось 'const'", startPos < tokens.Count ? tokens[startPos] : null);
                suppressGarbageErrors = true;
                headerInvalid = true;
                pos = startPos;
            }
            else
            {
                pos = idxConst + 1;
            }

            int idxVal = FindTokenAndCheckGarbage(pos, (int)TokenKind.Keyword, "val", "val", out garbage);
            if (idxVal == -1)
            {
                AddErrorAt("После 'const' ожидается 'val'", pos < tokens.Count ? tokens[pos] : null);
                suppressGarbageErrors = true;
                headerInvalid = true;
            }
            else
            {
                pos = idxVal + 1;
            }

            if (headerInvalid)
            {
                
                if (pos < endPos && tokens[pos].Code == (int)TokenKind.Identifier)
                {
                    decl.Children.Add(new AstNode { NodeType = "Identifier", Token = tokens[pos] });
                    pos++;
                }
                else
                {
                    AddErrorAt("Ожидается имя переменной", pos < tokens.Count ? tokens[pos] : null);
                    
                }

                
                if (pos < endPos && tokens[pos].Code == (int)TokenKind.Colon)
                {
                    pos++;
                }
                else
                {
                    AddErrorAt("Ожидается ':' перед типом", pos < tokens.Count ? tokens[pos] : null);
                }

                
                if (pos < endPos &&
                    tokens[pos].Code == (int)TokenKind.Keyword &&
                    tokens[pos].Lexeme == "Double")
                {
                    decl.Children.Add(new AstNode { NodeType = "Type", Token = tokens[pos] });
                    pos++;
                }
                else
                {
                    AddErrorAt("Ожидается тип Double", pos < tokens.Count ? tokens[pos] : null);
                }

                
                if (pos < endPos && tokens[pos].Code == (int)TokenKind.Assignment)
                {
                    pos++;
                }
                else
                {
                    AddErrorAt("Ожидается '='", pos < tokens.Count ? tokens[pos] : null);
                }


                
                if (pos < endPos &&
                    (tokens[pos].Code == (int)TokenKind.RealNumber ||
                     tokens[pos].Code == (int)TokenKind.UnsignedInteger))
                {
                    decl.Children.Add(new AstNode { NodeType = "Number", Token = tokens[pos] });
                    pos++;
                }
                else
                {
                    AddErrorAt("Ожидается число", pos < tokens.Count ? tokens[pos] : null);
                    if (pos < endPos) pos++;
                }

                
                if (endPos < tokens.Count && tokens[endPos].Code == (int)TokenKind.StatementEnd)
                {
                    pos = endPos + 1;
                }
                else
                {
                    AddErrorAt("Ожидается ';' в конце оператора", pos < tokens.Count ? tokens[pos] : null);
                    pos = endPos;
                }

                return decl;
            }


            int idxId = FindTokenAndCheckGarbage(pos, (int)TokenKind.Identifier, null, "имя переменной", out garbage);
            if (idxId == -1)
            {
                AddErrorAt("Ожидается имя переменной", pos < tokens.Count ? tokens[pos] : null);
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = "Identifier", Token = tokens[idxId] });
                pos = idxId + 1;
            }

            int idxColon = FindTokenAndCheckGarbage(pos, (int)TokenKind.Colon, null, "':'", out garbage);
            if (idxColon == -1)
            {
                AddErrorAt("Ожидается ':' перед типом", pos < tokens.Count ? tokens[pos] : null);
            }
            else
            {
                pos = idxColon + 1;
            }

            int idxDouble = FindTokenAndCheckGarbage(pos, (int)TokenKind.Keyword, "Double", "Double", out garbage);
            if (idxDouble == -1)
            {
                AddErrorAt("Ожидается тип Double", pos < tokens.Count ? tokens[pos] : null);
            }
            else
            {
                decl.Children.Add(new AstNode { NodeType = "Type", Token = tokens[idxDouble] });
                pos = idxDouble + 1;
            }

            int idxAssign = FindTokenAndCheckGarbage(pos, (int)TokenKind.Assignment, null, "'='", out garbage);
            if (idxAssign == -1)
            {
                AddErrorAt("Ожидается '='", pos < tokens.Count ? tokens[pos] : null);
            }
            else
            {
                pos = idxAssign + 1;

                
                while (pos < endPos && tokens[pos].Code == (int)TokenKind.Assignment)
                {
                    AddErrorAt("Лишний символ '='", tokens[pos]);
                    pos++; 
                }
            }


            int idxNumber = -1;
            bool reportedBadNumberFormat = false;

            for (int i = pos; i < endPos; i++)
            {
                if (tokens[i].Code == (int)TokenKind.RealNumber || tokens[i].Code == (int)TokenKind.UnsignedInteger)
                {
                    idxNumber = i;
                    break;
                }
            }

            if (idxNumber != -1)
            {
                var numTok = tokens[idxNumber];
                string lex = numTok?.Lexeme ?? "";

                if (System.Text.RegularExpressions.Regex.IsMatch(lex, @"^\d+\.\d+$"))
                {
                    decl.Children.Add(new AstNode { NodeType = "Number", Token = numTok });
                    pos = idxNumber + 1;
                }
                else
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(lex, @"^\d+\.$"))
                    {
                        AddErrorAt($"Недопустимый формат числа: '{lex}'. После точки ожидаются цифры.", numTok);
                        reportedBadNumberFormat = true;
                        pos = idxNumber + 1;
                    }
                    else
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(lex, @"^\d+$"))
                        {
                            AddErrorAt($"Ожидается число с дробной частью (через точку), найден '{lex}'", numTok);
                            reportedBadNumberFormat = true;
                            pos = idxNumber + 1;
                        }
                        else
                        {
                            AddErrorAt($"Недопустимый формат числа: '{lex}'", numTok);
                            reportedBadNumberFormat = true;
                            pos = idxNumber + 1;
                        }
                    }
                }
            }
            else
            {
                int idxLexError = -1;
                for (int i = pos; i < endPos; i++)
                {
                    if (tokens[i].Code == (int)TokenKind.Error)
                    {
                        idxLexError = i;
                        break;
                    }
                }

                if (idxLexError != -1)
                {
                    reportedBadNumberFormat = true;
                    pos = idxLexError + 1;
                }
                else
                {
                    int idxIdent = -1;
                    for (int i = pos; i < endPos; i++)
                    {
                        if (tokens[i].Code == (int)TokenKind.Identifier)
                        {
                            idxIdent = i;
                            break;
                        }
                    }

                    if (idxIdent != -1)
                    {
                        AddErrorAt($"Ожидается число, найден '{tokens[idxIdent].Lexeme}'", tokens[idxIdent]);
                        pos = idxIdent + 1;
                    }
                    else
                    {
                        AddErrorAt("Ожидается число", pos < tokens.Count ? tokens[pos] : (tokens.Count > 0 ? tokens[^1] : null));
                    }
                }
            }

            if (endPos < tokens.Count && tokens[endPos].Code == (int)TokenKind.StatementEnd)
            {
                if (pos < endPos && !reportedBadNumberFormat)
                {
                    AddErrorAt($"Неожиданный токен '{tokens[pos].Lexeme}' перед ';'", tokens[pos]);
                }
                pos = endPos + 1;
            }
            else
            {
                AddErrorAt("Ожидается ';' в конце оператора", pos < tokens.Count ? tokens[pos] : null);
                pos = endPos;
            }


            return decl;
        }

        private bool ExpectKeyword(string lexeme, string errorMessage)
        {
            if (!AtEnd() && Current.Code == (int)TokenKind.Keyword && Current.Lexeme == lexeme)
            {
                Next();
                return true;
            }

            ReportError(errorMessage);
            int temp = pos;
            while (temp < tokens.Count)
            {
                if (tokens[temp].Code == (int)TokenKind.Keyword && tokens[temp].Lexeme == lexeme)
                {
                    pos = temp + 1;
                    errorReportedForCurrentConstruct = false;
                    return true;
                }
                if (tokens[temp].Code == (int)TokenKind.StatementEnd) break;
                temp++;
            }
            return false;
        }

        public List<SyntaxError> GetErrors() => errors;

        public string PrintAst(AstNode node, string indent = "")
        {
            if (node == null) return "";
            string result = indent + "└─ " + node.NodeType;
            if (node.Token != null && !string.IsNullOrEmpty(node.Token.Lexeme))
                result += $" ({node.Token.Lexeme})";
            result += "\n";
            foreach (var child in node.Children)
                result += PrintAst(child, indent + "  ");
            return result;
        }
    }
}