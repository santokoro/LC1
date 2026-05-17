namespace LC1.Lab7.Optimizations
{
    /// <summary>
    /// После свёртки удаляет мёртвые временные и оставляет одну декларацию с константой.
    /// </summary>
    public sealed class TempEliminationOptimization : IOptimization
    {
        public string Name => "Устранение мёртвого кода и временных";

        public string Description =>
            "Удаляет неиспользуемые LOAD_CONST и арифметические инструкции, " +
            "подставляет итоговую константу напрямую в DECLARE_CONST.";

        public IrProgram Apply(IrProgram input)
        {
            var declare = input.Instructions.LastOrDefault(i => i.Opcode == IrOpcode.DeclareConst);
            if (declare == null || string.IsNullOrEmpty(declare.SourceTemp))
                return input.Clone();

            string typeName = declare.TypeName ?? "Double";
            var values = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var instr in input.Instructions)
            {
                if (instr.Opcode == IrOpcode.LoadConst &&
                    LiteralNormalizer.TryParseLiteral(instr.Constant ?? "", out double lit))
                    values[instr.Temp!] = lit;
                else if (!string.IsNullOrEmpty(instr.Temp) &&
                         IrEvaluator.TryEvaluate(instr, values, typeName, out double computed))
                    values[instr.Temp] = computed;
            }

            if (!values.TryGetValue(declare.SourceTemp, out double finalValue))
                return input.Clone();

            var result = new IrProgram();
            result.Instructions.Add(new IrInstruction
            {
                Opcode = IrOpcode.DeclareConst,
                Name = declare.Name,
                TypeName = declare.TypeName,
                Constant = IrEvaluator.FormatValue(finalValue, typeName)
            });

            IrGenerator.Renumber(result);
            return result;
        }
    }
}
