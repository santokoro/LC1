namespace LC1.Ast
{
    public sealed class SemanticError
    {
        public string Fragment { get; set; } = "";
        public string Message { get; set; } = "";
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
    }
}
