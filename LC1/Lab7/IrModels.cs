namespace LC1.Lab7
{
    public enum IrOpcode
    {
        LoadConst,
        Add,
        Sub,
        Mul,
        Div,
        Neg,
        DeclareConst
    }

    public sealed class IrInstruction
    {
        public int Index { get; set; }
        public IrOpcode Opcode { get; set; }
        public string? Temp { get; set; }
        public string? Constant { get; set; }
        public string? Left { get; set; }
        public string? Right { get; set; }
        public string? Operand { get; set; }
        public string? Name { get; set; }
        public string? TypeName { get; set; }
        public string? SourceTemp { get; set; }
    }

    public sealed class IrProgram
    {
        public List<IrInstruction> Instructions { get; } = new();

        public IrProgram Clone()
        {
            var copy = new IrProgram();
            foreach (var instr in Instructions)
            {
                copy.Instructions.Add(new IrInstruction
                {
                    Index = instr.Index,
                    Opcode = instr.Opcode,
                    Temp = instr.Temp,
                    Constant = instr.Constant,
                    Left = instr.Left,
                    Right = instr.Right,
                    Operand = instr.Operand,
                    Name = instr.Name,
                    TypeName = instr.TypeName,
                    SourceTemp = instr.SourceTemp
                });
            }
            return copy;
        }
    }
}
