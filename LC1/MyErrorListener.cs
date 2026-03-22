using Antlr4.Runtime;
using System.Collections.Generic;
using System.IO;

namespace LC1
{
    internal class MyErrorListener : IAntlrErrorListener<IToken>
    {
        public List<string> Errors { get; } = new();

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            Errors.Add($"Строка {line}, позиция {charPositionInLine}: {msg}");
        }
    }
}
