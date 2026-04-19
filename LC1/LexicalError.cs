using System;
using System.Collections.Generic;

namespace LC1
{
    public class LexicalError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
        public Token Token { get; set; }
    }

    public static class LexicalValidator
    {
        private static readonly HashSet<char> ForbiddenChars = new HashSet<char>
        {
            '@', '#', '%', '$', '^', '&', '~', '`', '?'
        };

        public static List<LexicalError> Validate(List<Token> tokens)
        {
            var errors = new List<LexicalError>();
            if (tokens == null || tokens.Count == 0) return errors;

            foreach (var t in tokens)
            {
                if (t == null) continue;

                if (t.Code == (int)TokenKind.Error)
                {
                    errors.Add(new LexicalError
                    {
                        Line = t.Line,
                        Column = t.Start,
                        Message = $"Недопустимый символ или лексема: '{t.Lexeme}'",
                        Token = t
                    });
                    continue;
                }

                if (!string.IsNullOrEmpty(t.Lexeme) && t.Lexeme.Length == 1 && ForbiddenChars.Contains(t.Lexeme[0]))
                {
                    errors.Add(new LexicalError
                    {
                        Line = t.Line,
                        Column = t.Start,
                        Message = $"Недопустимый символ '{t.Lexeme}' в исходном тексте",
                        Token = t
                    });
                }
            }

            return errors;
        }
    }
}
