namespace LC1.Core
{
    /// <summary>
    /// Рекурсивный спуск по грамматике:
    /// E → TA | A → ε | + TA | - TA | T → FB | B → ε | * FB | / FB | % FB | F → num | id | (E)
    /// </summary>
    public sealed class ExprRecursiveDescentParser
    {
        private readonly IReadOnlyList<ExprToken> _tokens;
        private readonly ExprParseResult _result;
        private int _pos;
        private int _tempCounter;

        private ExprRecursiveDescentParser(IReadOnlyList<ExprToken> tokens, ExprParseResult result)
        {
            _tokens = tokens;
            _result = result;
        }

        public static ExprParseResult Analyze(string source) =>
            AnalyzeTokens(ExprScanner.Analyze(source).Tokens);

        public static ExprParseResult AnalyzeTokens(IReadOnlyList<ExprToken> tokens)
        {
            var result = new ExprParseResult();
            var parser = new ExprRecursiveDescentParser(tokens, result);
            parser.Parse();
            return result;
        }

        private ExprToken Current => _pos < _tokens.Count ? _tokens[_pos] : _tokens[^1];
        private ExprTokenKind Kind => Current.Kind;

        private void Advance() => _pos++;

        private bool Match(ExprTokenKind kind)
        {
            if (Kind != kind)
                return false;
            Advance();
            return true;
        }

        private string NewTemp() => $"t{++_tempCounter}";

        private void AddError(string message, ExprToken? token = null)
        {
            var t = token ?? Current;
            _result.Errors.Add(new ExprParseError
            {
                Fragment = string.IsNullOrEmpty(t.Text) ? "(пусто)" : t.Text,
                Message = message,
                Line = t.Line,
                StartColumn = t.StartColumn,
                EndColumn = t.EndColumn
            });
        }

        private void EmitTetrad(string op, string arg1, string arg2, string result)
        {
            _result.Tetrads.Add(new Tetrad
            {
                Op = op,
                Arg1 = arg1,
                Arg2 = arg2,
                Result = result
            });
        }

        private sealed class SubExpr
        {
            public string Place { get; init; } = "";
            public List<string> Rpn { get; init; } = new();
            public bool HasIdentifier { get; init; }
        }

        private void Parse()
        {
            if (_tokens.Any(t => t.Kind == ExprTokenKind.Error))
                return;

            if (_tokens.Count == 0 || (_tokens.Count == 1 && Kind == ExprTokenKind.End))
            {
                AddError("ожидается выражение");
                return;
            }

            if (Kind == ExprTokenKind.Error)
            {
                AddError("недопустимая лексема", Current);
                return;
            }

            var expr = ParseE();
            if (_result.Errors.Count > 0)
                return;

            if (Kind != ExprTokenKind.End)
            {
                AddError($"лишние символы после выражения, ожидался конец ввода", Current);
                return;
            }

            _result.Rpn.AddRange(expr.Rpn);
            TryEvaluate(expr);
        }

        private SubExpr ParseE() => ParseA(ParseT());

        private SubExpr ParseA(SubExpr left)
        {
            while (true)
            {
                if (Kind == ExprTokenKind.Plus)
                {
                    Advance();
                    var right = ParseT();
                    left = CombineBinary("+", left, right);
                    continue;
                }

                if (Kind == ExprTokenKind.Minus)
                {
                    Advance();
                    var right = ParseT();
                    left = CombineBinary("-", left, right);
                    continue;
                }

                return left;
            }
        }

        private SubExpr ParseT() => ParseB(ParseF());

        private SubExpr ParseB(SubExpr left)
        {
            while (true)
            {
                if (Kind == ExprTokenKind.Multiply)
                {
                    Advance();
                    var right = ParseF();
                    left = CombineBinary("*", left, right);
                    continue;
                }

                if (Kind == ExprTokenKind.Divide)
                {
                    Advance();
                    var right = ParseF();
                    left = CombineBinary("/", left, right);
                    continue;
                }

                if (Kind == ExprTokenKind.Modulo)
                {
                    Advance();
                    var right = ParseF();
                    left = CombineBinary("%", left, right);
                    continue;
                }

                return left;
            }
        }

        private SubExpr CombineBinary(string op, SubExpr left, SubExpr right)
        {
            var temp = NewTemp();
            EmitTetrad(op, left.Place, right.Place, temp);

            var rpn = new List<string>(left.Rpn);
            rpn.AddRange(right.Rpn);
            rpn.Add(op);

            return new SubExpr
            {
                Place = temp,
                Rpn = rpn,
                HasIdentifier = left.HasIdentifier || right.HasIdentifier
            };
        }

        private SubExpr ParseF()
        {
            if (Kind == ExprTokenKind.Number)
            {
                var t = Current;
                Advance();
                return new SubExpr
                {
                    Place = t.Text,
                    Rpn = new List<string> { t.Text },
                    HasIdentifier = false
                };
            }

            if (Kind == ExprTokenKind.Identifier)
            {
                var t = Current;
                Advance();
                return new SubExpr
                {
                    Place = t.Text,
                    Rpn = new List<string> { t.Text },
                    HasIdentifier = true
                };
            }

            if (Kind == ExprTokenKind.LeftParen)
            {
                Advance();
                var inner = ParseE();

                if (Kind == ExprTokenKind.RightParen)
                {
                    Advance();
                    return inner;
                }

                if (Kind == ExprTokenKind.End)
                    AddError("ожидается ')'");
                else if (Kind == ExprTokenKind.Error)
                    AddError("недопустимая лексема", Current);
                else
                    AddError("ожидается ')'", Current);

                return inner;
            }

            if (Kind == ExprTokenKind.End)
                AddError("ожидается операнд (число, идентификатор или '(')");
            else if (Kind == ExprTokenKind.Error)
                AddError("недопустимая лексема", Current);
            else if (Kind is ExprTokenKind.Plus or ExprTokenKind.Minus or ExprTokenKind.Multiply
                     or ExprTokenKind.Divide or ExprTokenKind.Modulo or ExprTokenKind.RightParen)
                AddError("ожидается операнд (число, идентификатор или '(')", Current);
            else
                AddError("ожидается операнд", Current);

            return new SubExpr { Place = "?", Rpn = new List<string> { "?" } };
        }

        private void TryEvaluate(SubExpr expr)
        {
            if (expr.HasIdentifier)
            {
                _result.CanEvaluate = false;
                _result.EvaluationMessage = "вычисление доступно только для выражений из целых чисел (без идентификаторов)";
                return;
            }

            try
            {
                var stack = new Stack<long>();
                foreach (var item in _result.Rpn)
                {
                    if (long.TryParse(item, out long num))
                    {
                        stack.Push(num);
                        continue;
                    }

                    if (stack.Count < 2)
                        throw new InvalidOperationException("недостаточно операндов в ПОЛИЗ");

                    long b = stack.Pop();
                    long a = stack.Pop();
                    long value = item switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => b == 0 ? throw new DivideByZeroException() : a / b,
                        "%" => b == 0 ? throw new DivideByZeroException() : a % b,
                        _ => throw new InvalidOperationException($"неизвестная операция: {item}")
                    };
                    stack.Push(value);
                }

                if (stack.Count != 1)
                    throw new InvalidOperationException("некорректная ПОЛИЗ");

                _result.CanEvaluate = true;
                _result.EvaluatedValue = stack.Pop();
                _result.EvaluationMessage = null;
            }
            catch (DivideByZeroException)
            {
                _result.CanEvaluate = false;
                _result.EvaluationMessage = "деление на ноль";
            }
            catch (Exception ex)
            {
                _result.CanEvaluate = false;
                _result.EvaluationMessage = ex.Message;
            }
        }
    }
}
