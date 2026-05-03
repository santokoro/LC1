#pragma warning disable 0162
#pragma warning disable 0219
#pragma warning disable 1591
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using IErrorNode = Antlr4.Runtime.Tree.IErrorNode;
using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.1")]
[System.Diagnostics.DebuggerNonUserCode]
[System.CLSCompliant(false)]
public partial class KotlinConstBaseListener : IKotlinConstListener {
	public virtual void EnterProgram([NotNull] KotlinConstParser.ProgramContext context) { }
	public virtual void ExitProgram([NotNull] KotlinConstParser.ProgramContext context) { }
	public virtual void EnterDeclaration([NotNull] KotlinConstParser.DeclarationContext context) { }
	public virtual void ExitDeclaration([NotNull] KotlinConstParser.DeclarationContext context) { }
	public virtual void EnterNumber([NotNull] KotlinConstParser.NumberContext context) { }
	public virtual void ExitNumber([NotNull] KotlinConstParser.NumberContext context) { }

	public virtual void EnterEveryRule([NotNull] ParserRuleContext context) { }
	public virtual void ExitEveryRule([NotNull] ParserRuleContext context) { }
	public virtual void VisitTerminal([NotNull] ITerminalNode node) { }
	public virtual void VisitErrorNode([NotNull] IErrorNode node) { }
}
