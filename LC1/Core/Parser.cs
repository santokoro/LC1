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
                    result.Errors.Add(new ParseError { Fragment = string.Concat(tokens.Skip(start).Take(i - start).Select(x => x.Text)), Message = MsgConst, Line = line, StartColumn = tokens[start].StartColumn, EndColumn = tokens[i - 1].EndColumn });
                    continue;
                }

                if (t.Code == LexemeCode.Error)
                {
                    int start = i, line = t.Line;
                    var first = t;
                    string fragment = t.Text;
                    i++;
                    while (i < tokens.Count && tokens[i].Code == LexemeCode.Error && tokens[i].Line == line)
                    {
                        fragment += tokens[i].Text;
                        i++;
                    }
                    string errLex = phase == ParsePhase.ExpectNumber ? MsgBadDoubleLiteral : "недопустимая лексема";
                    result.Errors.Add(new ParseError { Fragment = fragment, Message = errLex, Line = line, StartColumn = first.StartColumn, EndColumn = tokens[i - 1].EndColumn });
                    if (phase == ParsePhase.ExpectNumber)
                        phase = ParsePhase.Done;
                    continue;
                }

                int consumed = CheckMatch(tokens, i, phase);
                if (consumed > 0)
                {
                    i += consumed;
                    phase = (phase == ParsePhase.Done) ? ParsePhase.ExpectStart : (ParsePhase)((int)phase + 1);
                    continue;
                }

                int startIndex = i;
                int syncIndex = -1;
                ParsePhase syncPhase = phase;
                bool hasFutureConst = tokens.Skip(i + 1).Any(x => x.Code == LexemeCode.KeywordConst);
                bool hasFutureVal = tokens.Skip(i + 1).Any(x => x.Code == LexemeCode.KeywordVal);

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
                        if (phase == ParsePhase.ExpectStart && (hasFutureConst || hasFutureVal) && ph == ParsePhase.ExpectId) continue;
                        if (phase == ParsePhase.ExpectVal && hasFutureVal && ph == ParsePhase.ExpectId) continue;
                        if (CheckMatch(tokens, j, ph) > 0)
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
                            var prev = startIndex > 0 ? tokens[startIndex - 1] : cur;
                            result.Errors.Add(new ParseError { Fragment = "", Message = errorMsg, Line = prev.Line, StartColumn = prev.EndColumn + 1, EndColumn = prev.EndColumn + 1 });
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
                if (last.Code != LexemeCode.Error)
                {
                    var lastErr = result.Errors.LastOrDefault();
                    var msg = GetExpectedMessage(phase);
                    if (lastErr == null || lastErr.Message != msg || lastErr.Line != last.Line)
                    {
                        result.Errors.Add(new ParseError { Fragment = "", Message = msg, Line = last.Line, StartColumn = last.EndColumn + 1, EndColumn = last.EndColumn + 1 });
                    }
                }
            }

            return result;
        }
    }
}