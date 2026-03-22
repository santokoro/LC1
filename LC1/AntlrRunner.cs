using Antlr4.Runtime;
using System.Collections.Generic;

namespace LC1
{
    internal static class AntlrRunner
    {
        public static (ParserRuleContext tree, List<string> errors) Run(string text)
        {
            var input = new AntlrInputStream(text);
            var lexer = new KotlinConstLexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new KotlinConstParser(tokens);

            var errorListener = new MyErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            var tree = parser.program();

            return (tree, errorListener.Errors);
        }
    }
}
