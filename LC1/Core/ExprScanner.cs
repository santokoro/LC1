using System.Text;

namespace LC1.Core
{
    public static class ExprScanner
    {
        private static bool IsLetter(char c) =>
            c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

        private static bool IsIdentChar(char c) =>
            IsLetter(c) || char.IsDigit(c) || c == '_';

        public static ExprScanResult Analyze(string source)
        {
            var result = new ExprScanResult();
            int line = 1, col = 1, i = 0;

            while (i < source.Length)
            {
                char ch = source[i];

                if (ch == '\r')
                {
                    i++;
                    if (i < source.Length && source[i] == '\n')
                        i++;
                    line++;
                    col = 1;
                    continue;
                }

                if (ch == '\n')
                {
                    line++;
                    col = 1;
                    i++;
                    continue;
                }

                if (ch is ' ' or '\t')
                {
                    i++;
                    col++;
                    continue;
                }

                int startCol = col;
                int startIndex = i;

                if (char.IsDigit(ch))
                {
                    var sb = new StringBuilder();
                    while (i < source.Length && char.IsDigit(source[i]))
                    {
                        sb.Append(source[i]);
                        i++;
                        col++;
                    }

                    if (i < source.Length && IsIdentChar(source[i]))
                    {
                        while (i < source.Length && IsIdentChar(source[i]))
                        {
                            sb.Append(source[i]);
                            i++;
                            col++;
                        }

                        AddError(result, sb.ToString(), line, startCol, col - 1);
                        continue;
                    }

                    string num = sb.ToString();
                    result.Tokens.Add(MakeToken(ExprTokenKind.Number, num, "целое число", line, startCol, col - 1));
                    continue;
                }

                if (IsLetter(ch) || ch == '_')
                {
                    var sb = new StringBuilder();
                    while (i < source.Length && IsIdentChar(source[i]))
                    {
                        sb.Append(source[i]);
                        i++;
                        col++;
                    }

                    string id = sb.ToString();
                    result.Tokens.Add(MakeToken(ExprTokenKind.Identifier, id, "идентификатор", line, startCol, col - 1));
                    continue;
                }

                ExprToken? single = ch switch
                {
                    '+' => MakeToken(ExprTokenKind.Plus, "+", "оператор +", line, startCol, startCol),
                    '-' => MakeToken(ExprTokenKind.Minus, "-", "оператор -", line, startCol, startCol),
                    '*' => MakeToken(ExprTokenKind.Multiply, "*", "оператор *", line, startCol, startCol),
                    '/' => MakeToken(ExprTokenKind.Divide, "/", "оператор /", line, startCol, startCol),
                    '%' => MakeToken(ExprTokenKind.Modulo, "%", "оператор %", line, startCol, startCol),
                    '(' => MakeToken(ExprTokenKind.LeftParen, "(", "скобка (", line, startCol, startCol),
                    ')' => MakeToken(ExprTokenKind.RightParen, ")", "скобка )", line, startCol, startCol),
                    _ => null
                };

                if (single != null)
                {
                    result.Tokens.Add(single);
                    i++;
                    col++;
                    continue;
                }

                i++;
                col++;
                AddError(result, ch.ToString(), line, startCol, startCol);
            }

            result.Tokens.Add(MakeToken(ExprTokenKind.End, "", "конец", line, col, col));
            return result;
        }

        private static void AddError(ExprScanResult result, string text, int line, int startCol, int endCol)
        {
            result.Tokens.Add(new ExprToken
            {
                Kind = ExprTokenKind.Error,
                Text = text,
                Type = "лексическая ошибка",
                Line = line,
                StartColumn = startCol,
                EndColumn = endCol
            });
        }

        private static ExprToken MakeToken(ExprTokenKind kind, string text, string type, int line, int startCol, int endCol) =>
            new()
            {
                Kind = kind,
                Text = text,
                Type = type,
                Line = line,
                StartColumn = startCol,
                EndColumn = endCol
            };
    }
}
