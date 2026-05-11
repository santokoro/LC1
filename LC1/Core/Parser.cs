using compiles_lab_1.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LC1.Core
{
    public enum ParsePhase
    {
        ExpectStart, ExpectVal, ExpectId, ExpectColon, ExpectDoubleKeyword, ExpectAssign, ExpectNumber, Done
    }

    public class ParseError
    {
        public string Fragment { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
    }

    public class ParseResult
    {
        public List<ParseError> Errors { get; } = new();
    }

    public static class Parser
    {
        private static string MsgConst => "ожидается ключевое слово \"const\"";
        private static string MsgVal => "ожидается ключевое слово \"val\"";
        private static string MsgDouble => "ожидается ключевое слово \"Double\"";
        private static string MsgColon => "ожидается символ ':'";
        private static string MsgAssign => "ожидается символ '='";
        private static string MsgNumber => "ожидается вещественное число";
        private static string MsgId => "ожидается идентификатор";
        private static string MsgSemi => "ожидается символ ';'";
        private static string MsgExtraAssign => "лишний символ '=' (ожидается вещественное число)";
        private static string MsgBadDoubleLiteral => "некорректная запись вещественного числа";

        private static bool IsValidDouble(Lexeme lex) => lex.Code == LexemeCode.DoubleLiteral;

        // Ключевое слово Double как тип — только сразу после ':' на той же строке.
        private static bool IsKeywordDoubleAsTypeAfterColon(IReadOnlyList<Lexeme> tokens, int index)
        {
            if (index <= 0 || index >= tokens.Count)
                return false;
            if (tokens[index].Code != LexemeCode.KeywordDouble)
                return false;
            var prev = tokens[index - 1];
            return prev.Code == LexemeCode.Colon && prev.Line == tokens[index].Line;
        }

        // Оператор присваивания в этой грамматике — только сразу после ключевого слова Double.
        private static bool IsAssignAfterDoubleTypeKeyword(IReadOnlyList<Lexeme> tokens, int index)
        {
            if (index <= 0 || index >= tokens.Count)
                return false;
            if (tokens[index].Code != LexemeCode.Assign)
                return false;
            var prev = tokens[index - 1];
            return prev.Code == LexemeCode.KeywordDouble && prev.Line == tokens[index].Line;
        }

        private static bool IsAssignOnlyFragment(string fragment) =>
            fragment.Length > 0 && fragment.All(c => c == '=');

        private static int CheckMatch(IReadOnlyList<Lexeme> tokens, int index, ParsePhase phase)
        {
            if (index >= tokens.Count) return 0;
            var t = tokens[index];

            switch (phase)
            {
                case ParsePhase.ExpectStart: return t.Code == LexemeCode.KeywordConst ? 1 : 0;
                case ParsePhase.ExpectVal: return t.Code == LexemeCode.KeywordVal ? 1 : 0;
                case ParsePhase.ExpectId: return (t.Code == LexemeCode.Identifier && t.Text != "val" && t.Text != "const") ? 1 : 0;
                case ParsePhase.ExpectColon: return t.Code == LexemeCode.Colon ? 1 : 0;
                case ParsePhase.ExpectDoubleKeyword: return t.Code == LexemeCode.KeywordDouble ? 1 : 0;
                case ParsePhase.ExpectAssign: return t.Code == LexemeCode.Assign ? 1 : 0;
                case ParsePhase.ExpectNumber:
                    if (t.Code == LexemeCode.DoubleLiteral && IsValidDouble(t)) return 1;
                    if (t.Code == LexemeCode.Minus && index + 1 < tokens.Count && tokens[index + 1].Code == LexemeCode.DoubleLiteral && IsValidDouble(tokens[index + 1])) return 2;
                    return 0;
                case ParsePhase.Done: return t.Code == LexemeCode.Semicolon ? 1 : 0;
                default: return 0;
            }
        }

        private static string GetExpectedMessage(ParsePhase phase) => phase switch
        {
            ParsePhase.ExpectStart => MsgConst,
            ParsePhase.ExpectVal => MsgVal,
            ParsePhase.ExpectId => MsgId,
            ParsePhase.ExpectColon => MsgColon,
            ParsePhase.ExpectDoubleKeyword => MsgDouble,
            ParsePhase.ExpectAssign => MsgAssign,
            ParsePhase.ExpectNumber => MsgNumber,
            ParsePhase.Done => MsgSemi,
            _ => ""
        };

        private static void AddErrorsAllMissingBeforeColon(ICollection<ParseError> errors, ParsePhase phaseBeforeColon, Lexeme colonLexeme)
        {
            int line = colonLexeme.Line;
            int col = colonLexeme.StartColumn;
            int endCol = colonLexeme.EndColumn;

            switch (phaseBeforeColon)
            {
                case ParsePhase.ExpectStart:
                    errors.Add(new ParseError { Fragment = ":", Message = MsgConst, Line = line, StartColumn = col, EndColumn = endCol });
                    errors.Add(new ParseError { Fragment = "", Message = MsgVal, Line = line, StartColumn = col, EndColumn = col });
                    errors.Add(new ParseError { Fragment = "", Message = MsgId, Line = line, StartColumn = col, EndColumn = col });
                    break;
                case ParsePhase.ExpectVal:
                    errors.Add(new ParseError { Fragment = ":", Message = MsgVal, Line = line, StartColumn = col, EndColumn = endCol });
                    errors.Add(new ParseError { Fragment = "", Message = MsgId, Line = line, StartColumn = col, EndColumn = col });
                    break;
                case ParsePhase.ExpectId:
                    errors.Add(new ParseError { Fragment = ":", Message = MsgId, Line = line, StartColumn = col, EndColumn = endCol });
                    break;
            }
        }

        private static void AddErrorsAllMissingFromPhaseThroughEnd(ICollection<ParseError> errors, ParsePhase fromPhase, int line, int startColumn, int endColumn)
        {
            for (int pi = (int)fromPhase; pi <= (int)ParsePhase.Done; pi++)
            {
                var ph = (ParsePhase)pi;
                errors.Add(new ParseError
                {
                    Fragment = "",
                    Message = GetExpectedMessage(ph),
                    Line = line,
                    StartColumn = startColumn,
                    EndColumn = endColumn
                });
            }
        }

        public static ParseResult Analyze(string source)
        {
            var scan = Scanner.Analyze(source);
            var tokens = scan.Lexemes;
            var result = new ParseResult();

            int i = 0;
            ParsePhase phase = ParsePhase.ExpectStart;

            while (i < tokens.Count)
            {
                var t = tokens[i];

                if (phase == ParsePhase.ExpectStart && t.Code == LexemeCode.Identifier && (i + 1 >= tokens.Count || tokens[i + 1].Code == LexemeCode.Semicolon))
                {
                    result.Errors.Add(new ParseError { Fragment = t.Text, Message = MsgConst, Line = t.Line, StartColumn = t.StartColumn, EndColumn = t.EndColumn });
                    int line = t.Line;
                    while (i < tokens.Count && tokens[i].Line == line && tokens[i].Code != LexemeCode.Semicolon) i++;
                    if (i < tokens.Count && tokens[i].Code == LexemeCode.Semicolon) i++;
                    phase = ParsePhase.ExpectStart;
                    continue;
                }

                if (phase == ParsePhase.ExpectStart && t.Code == LexemeCode.Semicolon)
                {
                    int start = i, line = t.Line;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Semicolon && tokens[i].Line == line) i++;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Error && tokens[i].Line == line) i++;
                    result.Errors.Add(new ParseError { Fragment = string.Concat(tokens.Skip(start).Take(i - start).Select(x => x.Text)), Message = MsgConst, Line = line, StartColumn = tokens[start].StartColumn, EndColumn = tokens[i - 1].EndColumn });
                    continue;
                }

                if ((phase == ParsePhase.ExpectStart || phase == ParsePhase.ExpectVal || phase == ParsePhase.ExpectId) && t.Code == LexemeCode.Colon)
                {
                    bool hasFutureColon = tokens.Skip(i + 1).Any(x => x.Line == t.Line && x.Code == LexemeCode.Colon);
                    if (hasFutureColon)
                    {
                        result.Errors.Add(new ParseError { Fragment = ":", Message = "лишний символ ':'", Line = t.Line, StartColumn = t.StartColumn, EndColumn = t.EndColumn });
                        i++;
                        continue;
                    }
                    AddErrorsAllMissingBeforeColon(result.Errors, phase, t);
                    i++;
                    phase = ParsePhase.ExpectDoubleKeyword;
                    continue;
                }

                if (phase == ParsePhase.ExpectAssign && t.Code == LexemeCode.Semicolon)
                {
                    int start = i;
                    int line = t.Line;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Semicolon && tokens[i].Line == line)
                        i++;
                    string semis = string.Concat(tokens.Skip(start).Take(i - start).Select(x => x.Text));
                    result.Errors.Add(new ParseError
                    {
                        Fragment = semis,
                        Message = MsgAssign,
                        Line = line,
                        StartColumn = tokens[start].StartColumn,
                        EndColumn = tokens[i - 1].EndColumn
                    });
                    continue;
                }

                if (phase == ParsePhase.ExpectId && t.Code == LexemeCode.Semicolon)
                {
                    int start = i;
                    int line = t.Line;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Semicolon && tokens[i].Line == line)
                        i++;
                    string semis = string.Concat(tokens.Skip(start).Take(i - start).Select(x => x.Text));
                    result.Errors.Add(new ParseError
                    {
                        Fragment = semis,
                        Message = MsgId,
                        Line = line,
                        StartColumn = tokens[start].StartColumn,
                        EndColumn = tokens[i - 1].EndColumn
                    });
                    continue;
                }

                if (phase == ParsePhase.ExpectColon && t.Code == LexemeCode.Semicolon)
                {
                    int start = i;
                    int line = t.Line;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Semicolon && tokens[i].Line == line)
                        i++;
                    string semis = string.Concat(tokens.Skip(start).Take(i - start).Select(x => x.Text));
                    result.Errors.Add(new ParseError
                    {
                        Fragment = semis,
                        Message = "ожидался символ ':', найдено ';'",
                        Line = line,
                        StartColumn = tokens[start].StartColumn,
                        EndColumn = tokens[i - 1].EndColumn
                    });
                    continue;
                }

                if (phase == ParsePhase.ExpectDoubleKeyword && t.Code == LexemeCode.Semicolon)
                {
                    int start = i;
                    int line = t.Line;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Semicolon && tokens[i].Line == line)
                        i++;
                    string semis = string.Concat(tokens.Skip(start).Take(i - start).Select(x => x.Text));
                    result.Errors.Add(new ParseError
                    {
                        Fragment = semis,
                        Message = MsgDouble,
                        Line = line,
                        StartColumn = tokens[start].StartColumn,
                        EndColumn = tokens[i - 1].EndColumn
                    });
                    continue;
                }

                if (phase == ParsePhase.ExpectNumber && t.Code == LexemeCode.Semicolon)
                {
                    int end = i;
                    while (end + 1 < tokens.Count
                           && tokens[end + 1].Line == tokens[i].Line
                           && tokens[end + 1].StartColumn == tokens[end].EndColumn + 1)
                    {
                        end++;
                    }

                    string fragment = string.Concat(tokens.Skip(i).Take(end - i + 1).Select(x => x.Text));
                    result.Errors.Add(new ParseError
                    {
                        Fragment = fragment,
                        Message = MsgBadDoubleLiteral,
                        Line = tokens[i].Line,
                        StartColumn = tokens[i].StartColumn,
                        EndColumn = tokens[end].EndColumn
                    });

                    i = end + 1;
                    phase = ParsePhase.ExpectStart;
                    continue;
                }

                if (t.Code == LexemeCode.Error)
                {
                    string fragment = t.Text;
                    var first = t;
                    i++;
                    string errLex = phase == ParsePhase.ExpectNumber
                        ? MsgBadDoubleLiteral
                        : GetExpectedMessage(phase);
                    result.Errors.Add(new ParseError { Fragment = fragment, Message = errLex, Line = first.Line, StartColumn = first.StartColumn, EndColumn = first.EndColumn });
                    if (phase == ParsePhase.ExpectNumber && i < tokens.Count)
                    {
                        if (CheckMatch(tokens, i, ParsePhase.ExpectNumber) <= 0
                            && tokens[i].Code == LexemeCode.Semicolon)
                            phase = ParsePhase.Done;
                    }
                    else if (phase != ParsePhase.Done)
                    {
                        bool nextMatchesCurrent = i < tokens.Count && CheckMatch(tokens, i, phase) > 0;
                        if (!nextMatchesCurrent)
                            phase = (ParsePhase)((int)phase + 1);
                    }
                    continue;
                }

                int consumed = CheckMatch(tokens, i, phase);
                if (consumed > 0)
                {
                    i += consumed;
                    if (phase == ParsePhase.Done)
                    {
                        phase = ParsePhase.ExpectStart;
                    }
                    else
                    {
                        phase = (ParsePhase)((int)phase + 1);
                    }
                    continue;
                }

                int startIndex = i;
                int syncIndex = -1;
                ParsePhase syncPhase = phase;
                int curLine = t.Line;
                bool hasFutureConst = tokens.Skip(i + 1).Any(x => x.Line == curLine && x.Code == LexemeCode.KeywordConst);
                bool hasFutureVal = tokens.Skip(i + 1).Any(x => x.Line == curLine && x.Code == LexemeCode.KeywordVal);

                for (int j = i; j < tokens.Count; j++)
                {
                    if (phase == ParsePhase.ExpectAssign)
                    {
                        if (tokens[j].Code == LexemeCode.DoubleLiteral && IsValidDouble(tokens[j]))
                        {
                            syncIndex = j; syncPhase = ParsePhase.ExpectNumber; break;
                        }
                        if (tokens[j].Code == LexemeCode.Minus && j + 1 < tokens.Count && tokens[j + 1].Code == LexemeCode.DoubleLiteral && IsValidDouble(tokens[j + 1]))
                        {
                            syncIndex = j; syncPhase = ParsePhase.ExpectNumber; break;
                        }
                    }

                    for (int p = (int)phase; p <= (int)ParsePhase.Done; p++)
                    {
                        var ph = (ParsePhase)p;
                        if (ph == ParsePhase.ExpectNumber) continue;
                        if (ph == ParsePhase.ExpectId && j == i && (int)phase < (int)ParsePhase.ExpectId)
                            continue;
                        if (phase == ParsePhase.ExpectStart && hasFutureConst && (int)ph > (int)ParsePhase.ExpectStart) continue;
                        if (phase == ParsePhase.ExpectStart && !hasFutureConst && hasFutureVal && (int)ph > (int)ParsePhase.ExpectVal) continue;
                        if (phase == ParsePhase.ExpectVal && hasFutureVal && (int)ph > (int)ParsePhase.ExpectVal) continue;

                        bool syncMatch = false;
                        if (ph == ParsePhase.ExpectDoubleKeyword && tokens[j].Code == LexemeCode.KeywordDouble)
                        {
                            if ((int)phase >= (int)ParsePhase.ExpectDoubleKeyword || IsKeywordDoubleAsTypeAfterColon(tokens, j))
                                syncMatch = true;
                        }
                        else if (ph == ParsePhase.ExpectAssign)
                        {
                            if (tokens[j].Code == LexemeCode.Assign && IsAssignAfterDoubleTypeKeyword(tokens, j))
                                syncMatch = true;
                        }
                        else if (CheckMatch(tokens, j, ph) > 0)
                        {
                            syncMatch = true;
                        }

                        if (syncMatch)
                        {
                            syncIndex = j; syncPhase = ph; break;
                        }
                    }
                    if (syncIndex != -1) break;
                }

                string errorMsg = GetExpectedMessage(phase);

                if (syncIndex != -1)
                {
                    if (syncIndex == startIndex)
                    {
                        var cur = tokens[startIndex];
                        bool skipRedundantConstAfterLexError =
                            phase == ParsePhase.ExpectStart
                            && startIndex > 0
                            && tokens[startIndex - 1].Line == cur.Line
                            && tokens[startIndex - 1].Code == LexemeCode.Error;

                        if (phase == ParsePhase.ExpectStart)
                        {
                            if (!skipRedundantConstAfterLexError)
                                result.Errors.Add(new ParseError { Fragment = "", Message = errorMsg, Line = cur.Line, StartColumn = cur.StartColumn, EndColumn = cur.StartColumn });
                        }
                        else
                        {
                            result.Errors.Add(new ParseError
                            {
                                Fragment = cur.Text,
                                Message = errorMsg,
                                Line = cur.Line,
                                StartColumn = cur.StartColumn,
                                EndColumn = cur.EndColumn
                            });
                        }

                        for (int mp = (int)phase + 1; mp < (int)syncPhase; mp++)
                        {
                            var missingPhase = (ParsePhase)mp;
                            result.Errors.Add(new ParseError
                            {
                                Fragment = "",
                                Message = GetExpectedMessage(missingPhase),
                                Line = cur.Line,
                                StartColumn = cur.StartColumn,
                                EndColumn = cur.StartColumn
                            });
                        }
                    }
                    else
                    {
                        int lastMuseumIndex = syncIndex - 1;

                        if (phase == ParsePhase.ExpectNumber)
                        {
                            while (lastMuseumIndex >= startIndex &&
                                   (tokens[lastMuseumIndex].Code == LexemeCode.DoubleLiteral ||
                                    tokens[lastMuseumIndex].Code == LexemeCode.Minus))
                            {
                                lastMuseumIndex--;
                            }
                        }

                        if (lastMuseumIndex >= startIndex)
                        {
                            var firstSkipped = tokens[startIndex];
                            var lastSkipped = tokens[lastMuseumIndex];
                            string fragment = string.Join("", tokens.GetRange(startIndex, lastMuseumIndex - startIndex + 1).Select(x => x.Text));
                            if (errorMsg == MsgNumber && IsAssignOnlyFragment(fragment))
                                errorMsg = MsgExtraAssign;
                            result.Errors.Add(new ParseError
                            {
                                Fragment = fragment,
                                Message = errorMsg,
                                Line = firstSkipped.Line,
                                StartColumn = firstSkipped.StartColumn,
                                EndColumn = lastSkipped.EndColumn
                            });
                        }
                        else
                        {
                            var cur = tokens[startIndex];
                            result.Errors.Add(new ParseError
                            {
                                Fragment = "",
                                Message = errorMsg,
                                Line = cur.Line,
                                StartColumn = cur.StartColumn,
                                EndColumn = cur.StartColumn
                            });
                        }

                        if (phase == ParsePhase.ExpectDoubleKeyword && syncPhase == ParsePhase.Done)
                        {
                            bool assignBetween = false;
                            for (int k = startIndex; k < syncIndex && k < tokens.Count; k++)
                            {
                                if (tokens[k].Code == LexemeCode.Assign)
                                {
                                    assignBetween = true;
                                    break;
                                }
                            }
                            if (!assignBetween)
                            {
                                var syncToken = tokens[syncIndex];
                                result.Errors.Add(new ParseError
                                {
                                    Fragment = "",
                                    Message = MsgAssign,
                                    Line = syncToken.Line,
                                    StartColumn = syncToken.StartColumn,
                                    EndColumn = syncToken.StartColumn
                                });
                            }
                        }

                    }
                    i = syncIndex;
                    phase = syncPhase;
                }
                else
                {
                    result.Errors.Add(new ParseError
                    {
                        Fragment = string.Join("", tokens.GetRange(startIndex, tokens.Count - startIndex).Select(x => x.Text)),
                        Message = errorMsg,
                        Line = tokens[startIndex].Line,
                        StartColumn = tokens[startIndex].StartColumn,
                        EndColumn = tokens[tokens.Count - 1].EndColumn
                    });
                    i = tokens.Count;
                }
            }

            if (phase != ParsePhase.ExpectStart && tokens.Count > 0)
            {
                var last = tokens[tokens.Count - 1];
                if (last.Code != LexemeCode.Error || phase >= ParsePhase.ExpectDoubleKeyword)
                {
                    bool skipTail =
                        phase == ParsePhase.Done
                        && result.Errors.Count > 0
                        && result.Errors[^1].Message == MsgSemi;

                    if (!skipTail)
                    {
                        int col = last.EndColumn + 1;
                        bool onlyConstInput =
                            tokens.Count == 1 &&
                            tokens[0].Code == LexemeCode.KeywordConst &&
                            phase == ParsePhase.ExpectVal;

                        if (onlyConstInput)
                        {
                            result.Errors.Add(new ParseError
                            {
                                Fragment = "",
                                Message = MsgVal,
                                Line = last.Line,
                                StartColumn = col,
                                EndColumn = col
                            });
                        }
                        else
                        {
                            ParsePhase fromPhase = phase;
                            if (result.Errors.Count > 0 && result.Errors[^1].Message == GetExpectedMessage(phase) && phase != ParsePhase.Done)
                                fromPhase = (ParsePhase)((int)phase + 1);

                            bool misplacedNumberInsteadOfType =
                                phase == ParsePhase.ExpectDoubleKeyword
                                && result.Errors.Count > 0
                                && result.Errors[^1].Message == MsgDouble
                                && tokens.Count > 0
                                && tokens[^1].Code == LexemeCode.DoubleLiteral;

                            if (misplacedNumberInsteadOfType)
                            {
                                result.Errors.Add(new ParseError
                                {
                                    Fragment = "",
                                    Message = MsgAssign,
                                    Line = last.Line,
                                    StartColumn = col,
                                    EndColumn = col
                                });
                                result.Errors.Add(new ParseError
                                {
                                    Fragment = "",
                                    Message = MsgSemi,
                                    Line = last.Line,
                                    StartColumn = col,
                                    EndColumn = col
                                });
                            }
                            else
                            {
                                AddErrorsAllMissingFromPhaseThroughEnd(result.Errors, fromPhase, last.Line, col, col);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}