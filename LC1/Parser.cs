using System;
using System.Collections.Generic;
using System.Linq;

namespace LC1
{
    // Класс для ошибок синтаксиса
    public class SyntaxError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
        public Token Token { get; set; }
    }

    // Класс для узлов дерева разбора
    public class AstNode
    {
        public string NodeType { get; set; }
        public Token Token { get; set; }
        public List<AstNode> Children { get; set; } = new List<AstNode>();

        public override string ToString()
        {
            if (Token != null)
                return $"{NodeType}: {Token.Lexeme}";
            return NodeType;
        }
    }

    public class Parser
    {
        private List<Token> tokens;
        private int position;
        private List<SyntaxError> errors;

        private Token Current => position < tokens.Count ? tokens[position] : null;

        public Parser(List<Token> tokens)
        {
            // Убираем пробелы - они не нужны для синтаксиса
            this.tokens = tokens.Where(t => t.Code != (int)TokenKind.Space).ToList();
            this.position = 0;
            this.errors = new List<SyntaxError>();
        }

        // ------------------------------------------------------------
        // <Программа> → <СписокОбъявлений>
        // ------------------------------------------------------------
        public AstNode ParseProgram()
        {
            var program = new AstNode { NodeType = "Программа" };

            while (Current != null)
            {
                // Пробуем распарсить объявление
                int beforePos = position;
                var decl = ParseDeclaration();

                if (decl != null)
                {
                    // Успешно распарсили
                    program.Children.Add(decl);
                }
                else
                {
                    // Не смогли распарсить - восстанавливаем позицию
                    position = beforePos;

                    // Сообщаем об ошибке и пропускаем токен
                    if (Current != null)
                    {
                        AddError($"Неожиданный токен: {Current.Lexeme}");
                        Next();
                    }
                }
            }

            return program;
        }

        // ------------------------------------------------------------
        // <Объявление> → ['const'] 'val' <Идентификатор> <НеобязательныйТип> '=' <ВещественноеЧисло> ';'
        // ------------------------------------------------------------
        private AstNode ParseDeclaration()
        {
            int startPos = position;

            if (Current == null) return null;

            var declNode = new AstNode { NodeType = "Объявление" };

            // --- РАЗБИРАЕМ const val (два слова подряд) ---

            // Проверяем на const
            if (CheckKeyword("const"))
            {
                // Добавляем const как модификатор
                declNode.Children.Add(new AstNode
                {
                    NodeType = "Модификатор",
                    Token = Current
                });
                Next(); // пропускаем const

                // После const обязательно должно быть val
                if (!CheckKeyword("val"))
                {
                    AddError("После 'const' ожидается 'val'");
                    position = startPos;
                    return null;
                }
            }

            // Проверяем на val (обязательно)
            if (!CheckKeyword("val"))
            {
                // Если это не val и не было const - это не объявление
                if (declNode.Children.Count == 0)
                    return null;

                // Если был const, а после него не val - уже обработали выше
                position = startPos;
                return null;
            }

            // Добавляем val
            declNode.Children.Add(new AstNode
            {
                NodeType = "КлючевоеСлово",
                Token = Current
            });
            Next(); // пропускаем val

            // --- ДАЛЬШЕ КАК ОБЫЧНО ---

            // Идентификатор
            if (!Check(TokenKind.Identifier))
            {
                AddError("Ожидается имя переменной");
                position = startPos;
                return null;
            }

            declNode.Children.Add(new AstNode
            {
                NodeType = "Идентификатор",
                Token = Current
            });
            Next();

            // Необязательный тип (: Double или : Float)
            if (Check(TokenKind.Colon))
            {
                var typeNode = ParseType();
                if (typeNode != null)
                    declNode.Children.Add(typeNode);
                else
                {
                    position = startPos;
                    return null;
                }
            }

            // Знак =
            if (!Check(TokenKind.Assignment))
            {
                AddError("Ожидается '='");
                position = startPos;
                return null;
            }

            declNode.Children.Add(new AstNode
            {
                NodeType = "Оператор",
                Token = Current
            });
            Next();

            // Вещественное число
            if (!Check(TokenKind.RealNumber))
            {
                AddError("Ожидается вещественное число");
                position = startPos;
                return null;
            }

            declNode.Children.Add(new AstNode
            {
                NodeType = "Число",
                Token = Current
            });
            Next();

            // Точка с запятой
            if (!Check(TokenKind.StatementEnd))
            {
                AddError("Ожидается ';' в конце объявления");
                position = startPos;
                return null;
            }

            declNode.Children.Add(new AstNode
            {
                NodeType = "Разделитель",
                Token = Current
            });
            Next();

            return declNode;
        }

        // ------------------------------------------------------------
        // <Тип> → ':' ('Double' | 'Float')
        // ------------------------------------------------------------
        private AstNode ParseType()
        {
            var typeNode = new AstNode { NodeType = "Тип" };

            // Двоеточие
            typeNode.Children.Add(new AstNode
            {
                NodeType = "Двоеточие",
                Token = Current
            });
            Next();

            // Проверяем тип (Double или Float)
            if (!CheckKeyword("Double") && !CheckKeyword("Float"))
            {
                AddError("Ожидается 'Double' или 'Float'");
                return null;
            }

            typeNode.Children.Add(new AstNode
            {
                NodeType = "ИмяТипа",
                Token = Current
            });
            Next();

            return typeNode;
        }

        // ------------------------------------------------------------
        // ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
        // ------------------------------------------------------------

        private bool Check(TokenKind kind)
        {
            return Current != null && Current.Code == (int)kind;
        }

        private bool CheckKeyword(string keyword)
        {
            return Current != null &&
                   Current.Code == (int)TokenKind.Keyword &&
                   Current.Lexeme == keyword;
        }

        private void Next()
        {
            position++;
        }

        private void AddError(string message)
        {
            if (Current != null)
            {
                errors.Add(new SyntaxError
                {
                    Line = Current.Line,
                    Column = Current.Start,
                    Message = message,
                    Token = Current
                });
            }
            else if (tokens.Count > 0)
            {
                var last = tokens.Last();
                errors.Add(new SyntaxError
                {
                    Line = last.Line,
                    Column = last.End + 1,
                    Message = message,
                    Token = null
                });
            }
        }

        public List<SyntaxError> GetErrors() => errors;

        // ------------------------------------------------------------
        // МЕТОД ДЛЯ ВЫВОДА ДЕРЕВА
        // ------------------------------------------------------------
        public string PrintAst(AstNode node, string indent = "")
        {
            if (node == null) return "";

            var result = indent + "└─ " + node.NodeType;

            if (node.Token != null)
                result += $" ({node.Token.Lexeme})";

            result += "\n";

            foreach (var child in node.Children)
            {
                result += PrintAst(child, indent + "   ");
            }

            return result;
        }
    }
}