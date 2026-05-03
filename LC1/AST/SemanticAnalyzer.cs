using System.Collections.Generic;

namespace LC1.Ast
{
    public static class SemanticAnalyzer
    {
        public static List<SemanticError> Analyze(ProgramNode program)
        {
            var errors = new List<SemanticError>();
            var declared = new HashSet<string>();

            foreach (var decl in program.Declarations)
            {
                if (!declared.Add(decl.Name))
                {
                    errors.Add(new SemanticError
                    {
                        Fragment = decl.Name,
                        Message = "повторное объявление идентификатора в той же области видимости",
                        Line = decl.Line,
                        StartColumn = decl.StartColumn,
                        EndColumn = decl.EndColumn
                    });
                }

                if (decl.Type is DoubleTypeNode dt && dt.TypeName != "Double")
                {
                    errors.Add(new SemanticError
                    {
                        Fragment = decl.Name,
                        Message = "несовместимость типов: объявленный тип должен быть Double",
                        Line = decl.Type.Line,
                        StartColumn = decl.Type.StartColumn,
                        EndColumn = decl.Type.EndColumn
                    });
                }

                if (decl.Value is DoubleLiteralNode lit)
                {
                    if (!double.IsFinite(lit.Value))
                    {
                        errors.Add(new SemanticError
                        {
                            Fragment = lit.RawText,
                            Message = "значение вне допустимых пределов для вещественного типа",
                            Line = lit.Line,
                            StartColumn = lit.StartColumn,
                            EndColumn = lit.EndColumn
                        });
                    }
                }
                else
                {
                    errors.Add(new SemanticError
                    {
                        Fragment = decl.Name,
                        Message = "несовместимость типов: инициализатор должен быть литералом Double",
                        Line = decl.Line,
                        StartColumn = decl.StartColumn,
                        EndColumn = decl.EndColumn
                    });
                }
            }

            return errors;
        }
    }
}
