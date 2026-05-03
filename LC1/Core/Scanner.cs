using compiles_lab_1.Core;
using System;
using System.Collections.Generic;

namespace LC1.Core
{
    public static class Scanner
    {
        static bool IsLatinLetter(char c) =>
            (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        static bool IsIdentContinue(char c) =>
            IsLatinLetter(c) || char.IsDigit(c) || c == '_';

        static bool IsKeywordBoundary(string source, int afterKeywordIndex) =>
            afterKeywordIndex >= source.Length || !IsIdentContinue(source[afterKeywordIndex]);

        public static ScanResult Analyze(string source)
        {
            var result = new ScanResult();
            int line = 1, col = 1, i = 0;

            while (i < source.Length)
            {
                char ch = source[i];

                if (ch == '\n') { line++; col = 1; i++; continue; }
                if (ch == ' ' || ch == '\t') { i++; col++; continue; }

                if (i + 5 <= source.Length && source.AsSpan(i, 5).SequenceEqual("const".AsSpan()))
                {
                    if (IsKeywordBoundary(source, i + 5))
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.KeywordConst,
                            Type = "ключевое слово",
                            Text = "const",
                            Line = line,
                            StartColumn = col,
                            EndColumn = col + 4
                        });
                        i += 5; col += 5;
                        continue;
                    }
                }

                if (i + 3 <= source.Length && source.AsSpan(i, 3).SequenceEqual("val".AsSpan()))
                {
                    if (IsKeywordBoundary(source, i + 3))
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.KeywordVal,
                            Type = "ключевое слово",
                            Text = "val",
                            Line = line,
                            StartColumn = col,
                            EndColumn = col + 2
                        });
                        i += 3; col += 3;
                        continue;
                    }
                }

                if (i + 6 <= source.Length && source.AsSpan(i, 6).SequenceEqual("Double".AsSpan()))
                {
                    if (IsKeywordBoundary(source, i + 6))
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.KeywordDouble,
                            Type = "ключевое слово",
                            Text = "Double",
                            Line = line,
                            StartColumn = col,
                            EndColumn = col + 5
                        });
                        i += 6; col += 6;
                        continue;
                    }
                }

                if (char.IsDigit(ch) || ch == '.')
                {
                    int startCol = col, startIndex = i;
                    bool hasDot = false;
                    bool hasDigit = false;
                    bool tooManyDots = false;

                    if (ch == '.')
                    {
                        i++; col++;
                        hasDot = true;
                        while (i < source.Length && char.IsDigit(source[i]))
                        {
                            hasDigit = true;
                            i++; col++;
                        }
                        if (i < source.Length && source[i] == '.')
                        {
                            tooManyDots = true;
                            while (i < source.Length && (source[i] == '.' || char.IsDigit(source[i])))
                            {
                                i++; col++;
                            }
                        }
                        if (!hasDigit) tooManyDots = true;
                    }
                    else if (char.IsDigit(ch))
                    {
                        hasDigit = true;
                        i++; col++;
                        while (i < source.Length)
                        {
                            char next = source[i];
                            if (char.IsDigit(next))
                            {
                                i++; col++;
                            }
                            else if (next == '.' && !hasDot)
                            {
                                hasDot = true;
                                i++; col++;
                            }
                            else if (next == '.' && hasDot)
                            {
                                tooManyDots = true;
                                i++; col++;
                                while (i < source.Length && (source[i] == '.' || char.IsDigit(source[i])))
                                {
                                    i++; col++;
                                }
                                break;
                            }
                            else break;
                        }
                    }

                    string number = source.Substring(startIndex, i - startIndex);
                    bool isValidDouble = hasDot && hasDigit && !tooManyDots;

                    if (isValidDouble)
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.DoubleLiteral,
                            Type = "вещественное число",
                            Text = number,
                            Line = line,
                            StartColumn = startCol,
                            EndColumn = col - 1
                        });
                    }
                    else
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.Error,
                            Type = "ошибка",
                            Text = number,
                            Line = line,
                            StartColumn = startCol,
                            EndColumn = col - 1
                        });
                    }
                    continue;
                }

                if (IsLatinLetter(ch) || ch == '_')
                {
                    int startCol = col, startIndex = i;
                    while (i < source.Length && (IsLatinLetter(source[i]) || char.IsDigit(source[i]) || source[i] == '_'))
                    {
                        i++; col++;
                    }
                    string identifier = source.Substring(startIndex, i - startIndex);

                    if (identifier == "const" || identifier == "val" || identifier == "Double")
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.Error,
                            Type = "ошибка",
                            Text = identifier,
                            Line = line,
                            StartColumn = startCol,
                            EndColumn = col - 1
                        });
                    }
                    else
                    {
                        result.Lexemes.Add(new Lexeme
                        {
                            Code = LexemeCode.Identifier,
                            Type = "идентификатор",
                            Text = identifier,
                            Line = line,
                            StartColumn = startCol,
                            EndColumn = col - 1
                        });
                    }
                    continue;
                }

                if (ch == ':' || ch == '=' || ch == ';' || ch == '-')
                {
                    LexemeCode code = ch == ':' ? LexemeCode.Colon : ch == '=' ? LexemeCode.Assign : ch == ';' ? LexemeCode.Semicolon : LexemeCode.Minus;
                    string type = ch == ':' ? "оператор объявления" : ch == '=' ? "оператор присваивания" : ch == ';' ? "конец оператора" : "оператор вычитания";
                    result.Lexemes.Add(new Lexeme
                    {
                        Code = code,
                        Type = type,
                        Text = ch.ToString(),
                        Line = line,
                        StartColumn = col,
                        EndColumn = col
                    });
                    i++; col++;
                    continue;
                }

                int errStartCol = col, errStartIndex = i;
                while (i < source.Length)
                {
                    char c = source[i];
                    bool isGood = char.IsDigit(c) || IsLatinLetter(c) || c == '_' || c == ':' || c == '=' || c == ';' || c == '-' || c == '.' || c == ' ' || c == '\t' || c == '\n';
                    if (isGood) break;
                    i++; col++;
                }
                string bad = source.Substring(errStartIndex, i - errStartIndex);
                result.Lexemes.Add(new Lexeme
                {
                    Code = LexemeCode.Error,
                    Type = "ошибка",
                    Text = bad,
                    Line = line,
                    StartColumn = errStartCol,
                    EndColumn = col - 1
                });
            }

            return result;
        }
    }
}