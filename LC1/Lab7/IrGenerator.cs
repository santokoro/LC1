namespace LC1.Lab7
{
    public static class IrGenerator
    {
        public static IrProgram Generate(ProgramAst ast)
        {
            var ir = new IrProgram();
            int tempCounter = 1;

            foreach (var decl in ast.Declarations)
            {
                string valueTemp = EmitExpr(decl.Initializer, ir, ref tempCounter);
                ir.Instructions.Add(new IrInstruction
                {
                    Opcode = IrOpcode.DeclareConst,
                    Name = decl.Name,
                    TypeName = decl.TypeName,
                    SourceTemp = valueTemp
                });
            }

            Renumber(ir);
            return ir;
        }

        private static string EmitExpr(ExprAst expr, IrProgram ir, ref int tempCounter)
        {
            switch (expr)
            {
                case NumberLiteralAst n:
                    return EmitLoadConst(n.SourceText, ir, ref tempCounter);
                case UnaryExprAst u:
                    string operand = EmitExpr(u.Operand, ir, ref tempCounter);
                    string neg = NextTemp(ref tempCounter);
                    ir.Instructions.Add(new IrInstruction
                    {
                        Opcode = IrOpcode.Neg,
                        Temp = neg,
                        Operand = operand
                    });
                    return neg;
                case BinaryExprAst b:
                    string left = EmitExpr(b.Left, ir, ref tempCounter);
                    string right = EmitExpr(b.Right, ir, ref tempCounter);
                    string result = NextTemp(ref tempCounter);
                    ir.Instructions.Add(new IrInstruction
                    {
                        Opcode = b.Op switch
                        {
                            BinaryOp.Add => IrOpcode.Add,
                            BinaryOp.Sub => IrOpcode.Sub,
                            BinaryOp.Mul => IrOpcode.Mul,
                            BinaryOp.Div => IrOpcode.Div,
                            _ => IrOpcode.Add
                        },
                        Temp = result,
                        Left = left,
                        Right = right
                    });
                    return result;
                default:
                    throw new InvalidOperationException("Неизвестный узел выражения");
            }
        }

        private static string EmitLoadConst(string value, IrProgram ir, ref int tempCounter)
        {
            string temp = NextTemp(ref tempCounter);
            ir.Instructions.Add(new IrInstruction
            {
                Opcode = IrOpcode.LoadConst,
                Temp = temp,
                Constant = value
            });
            return temp;
        }

        private static string NextTemp(ref int counter) => $"t{counter++}";

        internal static void Renumber(IrProgram ir)
        {
            for (int i = 0; i < ir.Instructions.Count; i++)
                ir.Instructions[i].Index = i + 1;
        }
    }
}
