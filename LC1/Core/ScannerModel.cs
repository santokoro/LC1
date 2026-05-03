namespace compiles_lab_1.Core
{
    public enum LexemeCode
    {
        DoubleLiteral = 1,
        KeywordDouble = 2,
        KeywordConst = 3,
        KeywordVal = 4,
        Identifier = 5,
        Colon = 6,
        Assign = 7,
        Semicolon = 8,
        Minus = 9,
        Error = 11
    }

    public class Lexeme
    {
        public LexemeCode Code { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
    }

    public class ScanResult
    {
        public List<Lexeme> Lexemes { get; } = new();
    }
}