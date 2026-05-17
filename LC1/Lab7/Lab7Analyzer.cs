using LC1.Lab7.Optimizations;
using System.Text;

namespace LC1.Lab7
{
    public sealed class Lab7Result
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public string AstText { get; init; } = "";
        public string InitialIrText { get; init; } = "";
        public List<Lab7OptimizationStep> Steps { get; } = new();
    }

    public sealed class Lab7OptimizationStep
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string InputIrText { get; init; }
        public required string OutputIrText { get; init; }
    }

    public static class Lab7Analyzer
    {
        private static readonly IOptimization[] Optimizations =
        {
            new ConstantFoldingOptimization(),
            new TempEliminationOptimization()
        };

        public static Lab7Result Analyze(string source)
        {
            var (tree, errors) = AntlrRunner.Run(source);
            if (errors.Count > 0)
            {
                return new Lab7Result
                {
                    Success = false,
                    ErrorMessage = "Синтаксическая ошибка (ANTLR):\n" + string.Join("\n", errors)
                };
            }

            if (tree is not KotlinConstParser.ProgramContext program)
            {
                return new Lab7Result
                {
                    Success = false,
                    ErrorMessage = "Не удалось построить дерево разбора."
                };
            }

            if (program.declaration().Length == 0)
            {
                return new Lab7Result
                {
                    Success = false,
                    ErrorMessage = "Введите объявление вида: const val id: Double = число;"
                };
            }

            var ast = AstBuilder.Build(program);
            var ir = IrGenerator.Generate(ast);

            var result = new Lab7Result
            {
                Success = true,
                AstText = AstFormatter.Format(ast),
                InitialIrText = IrFormatter.Format(ir)
            };

            var current = ir;
            foreach (var opt in Optimizations)
            {
                string inputText = IrFormatter.Format(current);
                var output = opt.Apply(current);
                result.Steps.Add(new Lab7OptimizationStep
                {
                    Name = opt.Name,
                    Description = opt.Description,
                    InputIrText = inputText,
                    OutputIrText = IrFormatter.Format(output)
                });
                current = output;
            }

            return result;
        }

        public static string FormatReport(Lab7Result result)
        {
            if (!result.Success)
                return result.ErrorMessage ?? "Ошибка анализа";

            var sb = new StringBuilder();
            sb.AppendLine("=== AST (лабораторная 5) ===");
            sb.AppendLine(result.AstText);
            sb.AppendLine();
            sb.AppendLine("=== Исходное IR (трёхадресный код) ===");
            sb.AppendLine(result.InitialIrText);

            for (int i = 0; i < result.Steps.Count; i++)
            {
                var step = result.Steps[i];
                sb.AppendLine();
                sb.AppendLine($"=== Оптимизация {i + 1}: {step.Name} ===");
                sb.AppendLine(step.Description);
                sb.AppendLine();
                sb.AppendLine("--- Входной IR ---");
                sb.AppendLine(step.InputIrText);
                sb.AppendLine();
                sb.AppendLine("--- Выходной IR ---");
                sb.AppendLine(step.OutputIrText);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
