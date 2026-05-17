using System.Text;

namespace LC1.Lab7
{
    public static class IrFormatter
    {
        public static string Format(IrProgram ir)
        {
            var sb = new StringBuilder();
            foreach (var instr in ir.Instructions)
                sb.AppendLine(FormatInstruction(instr));
            return sb.ToString().TrimEnd();
        }

        public static string FormatInstruction(IrInstruction instr) => instr.Opcode switch
        {
            IrOpcode.LoadConst => $"{instr.Index}: {instr.Temp} = LOAD_CONST {instr.Constant}",
            IrOpcode.Add => $"{instr.Index}: {instr.Temp} = ADD {instr.Left}, {instr.Right}",
            IrOpcode.Sub => $"{instr.Index}: {instr.Temp} = SUB {instr.Left}, {instr.Right}",
            IrOpcode.Mul => $"{instr.Index}: {instr.Temp} = MUL {instr.Left}, {instr.Right}",
            IrOpcode.Div => $"{instr.Index}: {instr.Temp} = DIV {instr.Left}, {instr.Right}",
            IrOpcode.Neg => $"{instr.Index}: {instr.Temp} = NEG {instr.Operand}",
            IrOpcode.DeclareConst when !string.IsNullOrEmpty(instr.Constant) =>
                $"{instr.Index}: DECLARE_CONST {instr.Name}, {instr.TypeName}, {instr.Constant}",
            IrOpcode.DeclareConst =>
                $"{instr.Index}: DECLARE_CONST {instr.Name}, {instr.TypeName}, {instr.SourceTemp}",
            _ => $"{instr.Index}: ???"
        };
    }
}
