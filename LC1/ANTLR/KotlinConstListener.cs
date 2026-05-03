#pragma warning disable 0162
#pragma warning disable 0219
#pragma warning disable 1591
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using IParseTreeListener = Antlr4.Runtime.Tree.IParseTreeListener;
using IToken = Antlr4.Runtime.IToken;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.1")]
[System.CLSCompliant(false)]
public interface IKotlinConstListener : IParseTreeListener {
	void EnterProgram([NotNull] KotlinConstParser.ProgramContext context);
	void ExitProgram([NotNull] KotlinConstParser.ProgramContext context);
	void EnterDeclaration([NotNull] KotlinConstParser.DeclarationContext context);
	void ExitDeclaration([NotNull] KotlinConstParser.DeclarationContext context);
	void EnterNumber([NotNull] KotlinConstParser.NumberContext context);
	void ExitNumber([NotNull] KotlinConstParser.NumberContext context);
}
