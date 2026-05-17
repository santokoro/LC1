namespace LC1.Core
{
    public enum ExprTokenKind
    {
        Number = 1,
        Identifier = 2,
        Plus = 3,
        Minus = 4,
        Multiply = 5,
        Divide = 6,
        Modulo = 7,
        LeftParen = 8,
        RightParen = 9,
        End = 10,
        Error = 11
    }

    public sealed class ExprToken
    {
        public ExprTokenKind Kind { get; init; }
        public string Text { get; init; } = "";
        public string Type { get; init; } = "";
        public int Line { get; init; }
        public int StartColumn { get; init; }
        public int EndColumn { get; init; }
    }

    public sealed class ExprScanResult
    {
        public List<ExprToken> Tokens { get; } = new();
    }

    public sealed class Tetrad
    {
        public string Op { get; init; } = "";
        public string Arg1 { get; init; } = "";
        public string Arg2 { get; init; } = "";
        public string Result { get; init; } = "";
    }

    public sealed class ExprParseError
    {
        public string Fragment { get; init; } = "";
        public string Message { get; init; } = "";
        public int Line { get; init; }
        public int StartColumn { get; init; }
        public int EndColumn { get; init; }
    }

    public sealed class ExprParseResult
    {
        public List<ExprParseError> Errors { get; } = new();
        public List<Tetrad> Tetrads { get; } = new();
        public List<string> Rpn { get; } = new();
        public string RpnText => string.Join(" ", Rpn);
        public bool CanEvaluate { get; set; }
        public long? EvaluatedValue { get; set; }
        public string? EvaluationMessage { get; set; }
        public bool IsSuccess => Errors.Count == 0;
    }
}
