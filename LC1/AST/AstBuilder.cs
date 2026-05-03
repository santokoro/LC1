using compiles_lab_1.Core;
using System.Collections.Generic;
using System.Globalization;

namespace LC1.Ast
{
    public static class AstBuilder
    {
        public static ProgramNode? TryBuild(IReadOnlyList<Lexeme> tokens, out string? errorMessage)
        {
            errorMessage = null;
            var decls = new List<ConstDeclNode>();
            int i = 0;

            while (i < tokens.Count)
            {
                if (!TryParseDeclaration(tokens, ref i, out var decl, out var err))
                {
                    errorMessage = err;
                    return null;
                }

                decls.Add(decl);
            }

            var program = new ProgramNode();
            foreach (var d in decls)
                program.Declarations.Add(d);

            if (decls.Count > 0)
            {
                program.Line = decls[0].Line;
                program.StartColumn = decls[0].StartColumn;
                program.EndColumn = decls[^1].EndColumn;
            }

            return program;
        }

        private static bool TryParseDeclaration(IReadOnlyList<Lexeme> tokens, ref int i, out ConstDeclNode decl, out string? error)
        {
            decl = null!;
            error = null;

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.KeywordConst)
            {
                error = "ожидается ключевое слово \"const\"";
                return false;
            }

            var tConst = tokens[i++];

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.KeywordVal)
            {
                error = "ожидается ключевое слово \"val\"";
                return false;
            }

            var tVal = tokens[i++];

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.Identifier)
            {
                error = "ожидается идентификатор";
                return false;
            }

            var idLex = tokens[i++];

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.Colon)
            {
                error = "ожидается символ ':'";
                return false;
            }

            _ = tokens[i++];

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.KeywordDouble)
            {
                error = "ожидается ключевое слово \"Double\"";
                return false;
            }

            var tDouble = tokens[i++];

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.Assign)
            {
                error = "ожидается символ '='";
                return false;
            }

            _ = tokens[i++];

            Lexeme? minusLex = null;
            if (i < tokens.Count && tokens[i].Code == LexemeCode.Minus)
            {
                minusLex = tokens[i];
                i++;
            }

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.DoubleLiteral)
            {
                error = "ожидается вещественное число";
                return false;
            }

            var numLex = tokens[i++];

            if (i >= tokens.Count || tokens[i].Code != LexemeCode.Semicolon)
            {
                error = "ожидается символ ';'";
                return false;
            }

            var semi = tokens[i++];

            if (!double.TryParse(numLex.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                error = "некорректное вещественное число";
                return false;
            }

            if (minusLex != null)
                parsed = -parsed;

            var constKw = new KeywordModifierNode
            {
                Line = tConst.Line,
                StartColumn = tConst.StartColumn,
                EndColumn = tConst.EndColumn,
                Keyword = "const"
            };

            var valKw = new KeywordModifierNode
            {
                Line = tVal.Line,
                StartColumn = tVal.StartColumn,
                EndColumn = tVal.EndColumn,
                Keyword = "val"
            };

            var idNode = new IdentifierNode
            {
                Line = idLex.Line,
                StartColumn = idLex.StartColumn,
                EndColumn = idLex.EndColumn,
                Name = idLex.Text
            };

            var typeNode = new DoubleTypeNode
            {
                Line = tDouble.Line,
                StartColumn = tDouble.StartColumn,
                EndColumn = tDouble.EndColumn,
                TypeName = "Double"
            };

            int litStart = minusLex?.StartColumn ?? numLex.StartColumn;
            int litEnd = numLex.EndColumn;

            var valNode = new DoubleLiteralNode
            {
                Line = numLex.Line,
                StartColumn = litStart,
                EndColumn = litEnd,
                Value = parsed,
                RawText = numLex.Text
            };

            decl = new ConstDeclNode
            {
                Line = tConst.Line,
                StartColumn = tConst.StartColumn,
                EndColumn = semi.EndColumn,
                ConstKeyword = constKw,
                ValKeyword = valKw,
                Identifier = idNode,
                Type = typeNode,
                Value = valNode
            };

            return true;
        }
    }
}
