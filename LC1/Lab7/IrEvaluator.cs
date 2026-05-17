namespace LC1.Lab7
{
    internal static class IrEvaluator
    {
        private const double Eps = 1e-12;

        public static bool TryGetValue(string temp, IReadOnlyDictionary<string, double> values, out double result) =>
            values.TryGetValue(temp, out result);

        public static bool TryEvaluate(IrInstruction instr, IReadOnlyDictionary<string, double> values, string typeName, out double result)
        {
            result = 0;
            switch (instr.Opcode)
            {
                case IrOpcode.LoadConst:
                    return LiteralNormalizer.TryParseLiteral(instr.Constant ?? "", out result);
                case IrOpcode.Neg:
                    if (!TryGetValue(instr.Operand!, values, out double v)) return false;
                    result = -v;
                    return true;
                case IrOpcode.Add:
                    if (!TryGetValue(instr.Left!, values, out double a) || !TryGetValue(instr.Right!, values, out double b)) return false;
                    result = a + b;
                    return true;
                case IrOpcode.Sub:
                    if (!TryGetValue(instr.Left!, values, out double l) || !TryGetValue(instr.Right!, values, out double r)) return false;
                    result = l - r;
                    return true;
                case IrOpcode.Mul:
                    if (!TryGetValue(instr.Left!, values, out double x) || !TryGetValue(instr.Right!, values, out double y)) return false;
                    result = x * y;
                    return true;
                case IrOpcode.Div:
                    if (!TryGetValue(instr.Left!, values, out double p) || !TryGetValue(instr.Right!, values, out double q) || Math.Abs(q) < Eps) return false;
                    result = p / q;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryAlgebraicSimplify(IrInstruction instr, IReadOnlyDictionary<string, double> values, out double result)
        {
            result = 0;
            switch (instr.Opcode)
            {
                case IrOpcode.Add:
                    if (TryGetValue(instr.Right!, values, out double r) && IsZero(r) && TryGetValue(instr.Left!, values, out result))
                        return true;
                    if (TryGetValue(instr.Left!, values, out double a) && IsZero(a) && TryGetValue(instr.Right!, values, out result))
                        return true;
                    return false;
                case IrOpcode.Sub:
                    if (TryGetValue(instr.Right!, values, out double s) && IsZero(s) && TryGetValue(instr.Left!, values, out result))
                        return true;
                    return false;
                case IrOpcode.Mul:
                    if ((TryGetValue(instr.Left!, values, out double x) && IsZero(x)) ||
                        (TryGetValue(instr.Right!, values, out double y) && IsZero(y)))
                    {
                        result = 0;
                        return true;
                    }
                    if (TryGetValue(instr.Left!, values, out double one) && IsOne(one) && TryGetValue(instr.Right!, values, out result))
                        return true;
                    if (TryGetValue(instr.Right!, values, out double oneR) && IsOne(oneR) && TryGetValue(instr.Left!, values, out result))
                        return true;
                    return false;
                default:
                    return false;
            }
        }

        public static string FormatValue(double value, string typeName)
        {
            if (typeName == "Double")
                return LiteralNormalizer.FormatDouble(value);
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool IsZero(double v) => Math.Abs(v) < Eps;
        private static bool IsOne(double v) => Math.Abs(v - 1) < Eps;
    }
}
