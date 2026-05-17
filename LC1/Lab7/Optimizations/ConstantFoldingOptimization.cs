namespace LC1.Lab7.Optimizations
{
    /// <summary>
    /// Свёртка констант: вычисление выражений на этапе компиляции.
    /// </summary>
    public sealed class ConstantFoldingOptimization : IOptimization
    {
        public string Name => "Свёртка констант (constant folding)";

        public string Description =>
            "Вычисляет операции над известными константами: +, -, *, /, унарный минус, " +
            "а также упрощения x*0=0, x+0=x, x-0=x, x*1=x.";

        public IrProgram Apply(IrProgram input)
        {
            string typeName = input.Instructions
                .FirstOrDefault(i => i.Opcode == IrOpcode.DeclareConst)?.TypeName ?? "Double";

            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            var output = new IrProgram();

            foreach (var instr in input.Instructions)
            {
                if (instr.Opcode == IrOpcode.DeclareConst)
                {
                    output.Instructions.Add(Clone(instr));
                    continue;
                }

                if (instr.Opcode == IrOpcode.LoadConst &&
                    LiteralNormalizer.TryParseLiteral(instr.Constant ?? "", out double lit))
                {
                    values[instr.Temp!] = lit;
                    string normalized = IrEvaluator.FormatValue(lit, typeName);
                    output.Instructions.Add(new IrInstruction
                    {
                        Opcode = IrOpcode.LoadConst,
                        Temp = instr.Temp,
                        Constant = normalized
                    });
                    continue;
                }

                if (TryFold(instr, values, typeName, out double folded))
                {
                    values[instr.Temp!] = folded;
                    output.Instructions.Add(new IrInstruction
                    {
                        Opcode = IrOpcode.LoadConst,
                        Temp = instr.Temp,
                        Constant = IrEvaluator.FormatValue(folded, typeName)
                    });
                }
                else
                {
                    output.Instructions.Add(Clone(instr));
                }
            }

            IrGenerator.Renumber(output);
            return output;
        }

        private static bool TryFold(IrInstruction instr, Dictionary<string, double> values, string typeName, out double folded)
        {
            if (IrEvaluator.TryAlgebraicSimplify(instr, values, out folded))
                return true;
            return IrEvaluator.TryEvaluate(instr, values, typeName, out folded);
        }

        private static IrInstruction Clone(IrInstruction instr) => new()
        {
            Opcode = instr.Opcode,
            Temp = instr.Temp,
            Constant = instr.Constant,
            Left = instr.Left,
            Right = instr.Right,
            Operand = instr.Operand,
            Name = instr.Name,
            TypeName = instr.TypeName,
            SourceTemp = instr.SourceTemp
        };
    }
}
