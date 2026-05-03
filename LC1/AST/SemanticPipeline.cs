using compiles_lab_1.Core;
using System.Collections.Generic;
using System.Text;

namespace LC1.Ast
{
    public sealed class SemanticPipelineResult
    {
        public ProgramNode? Program { get; init; }
        public List<SemanticError> SemanticErrors { get; init; } = new();
        public string AstText { get; init; } = "";

        public string BuildFullReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Абстрактное синтаксическое дерево (AST):");
            sb.AppendLine();
            sb.AppendLine(AstText);
            sb.AppendLine();
            sb.AppendLine("Семантический анализ:");
            if (SemanticErrors.Count == 0)
                sb.AppendLine("Семантических ошибок нет.");
            else
            {
                foreach (var e in SemanticErrors)
                {
                    sb.Append("  строка ").Append(e.Line).Append(", ")
                        .Append(e.StartColumn).Append("-").Append(e.EndColumn)
                        .Append(": ").AppendLine(e.Message);
                    if (!string.IsNullOrEmpty(e.Fragment))
                        sb.Append("    фрагмент: \"").Append(e.Fragment).AppendLine("\"");
                }
            }

            sb.AppendLine();
            sb.Append("Правило 4 (использование идентификаторов): в инициализаторе допускаются только литералы; ");
            sb.AppendLine("ссылок на другие идентификаторы нет — проверка тривиальна.");
            sb.AppendLine();
            sb.Append("Количество семантических ошибок: ").AppendLine(SemanticErrors.Count.ToString());
            return sb.ToString();
        }
    }

    public static class SemanticPipeline
    {
        public static SemanticPipelineResult BuildFromTokens(IReadOnlyList<Lexeme> tokens)
        {
            var program = AstBuilder.TryBuild(tokens, out var astErr);
            if (program == null)
            {
                return new SemanticPipelineResult
                {
                    AstText = "Не удалось построить AST: " + (astErr ?? "неизвестная ошибка"),
                    SemanticErrors = new List<SemanticError>()
                };
            }

            var astText = AstTextFormat.FormatProgram(program);
            var sem = SemanticAnalyzer.Analyze(program);
            return new SemanticPipelineResult
            {
                Program = program,
                AstText = astText,
                SemanticErrors = sem
            };
        }

        public static SemanticPipelineResult SkippedDueToSyntaxErrors() =>
            new SemanticPipelineResult
            {
                AstText = "Синтаксические ошибки — построение AST и семантический анализ не выполняются.",
                SemanticErrors = new List<SemanticError>()
            };
    }
}
