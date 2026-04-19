using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LC1
{


    public enum TokenKind
    {
        UnsignedInteger = 1,
        Identifier = 2,
        RealNumber = 3,

        Assignment = 10,
        Space = 11,
        Keyword = 14,
        StatementEnd = 16,
        Colon = 17,

        Plus = 20,
        Minus = 21,
        Multiply = 22,
        Divide = 23,

        Error = 99
    }

    public class Token
    {
        public int Code { get; set; }
        public string Type { get; set; } = "";
        public string Lexeme { get; set; } = "";
        public int Line { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public bool IsError { get; set; }

        public string Location => $"строка {Line}, {Start}-{End}";
    }



    public partial class MainWindow : Window
    {
        private bool isTextChanged = false;
        private string? currentFilePath = null;

        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private bool isInternalChange = false;

        private static readonly string[] Keywords = { "val", "const", "Double", "Float" };

        public MainWindow()
        {
            InitializeComponent();
            ApplyFontSize(14);

            EditorTextBox.AddHandler(ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler(EditorTextBox_ScrollChanged));

            EditorTextBox.TextChanged += (s, e) =>
            {
                if (!isInternalChange)
                {
                    undoStack.Push(EditorTextBox.Text);
                    redoStack.Clear();
                    isTextChanged = true;
                }
            };

            EditorTextBox.SelectionChanged += (s, e) => UpdateStatusBar();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (s, e) => NewFile()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (s, e) => OpenFile()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (s, e) => SaveFile()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, (s, e) => SaveFileAs()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (s, e) => Undo()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, (s, e) => Redo()));
            CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, (s, e) => RunCompiler()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => MenuExit_Click(s, e)));
        }



        private void NewFile()
        {
            undoStack.Clear();
            redoStack.Clear();
            undoStack.Push("");

            if (isTextChanged)
            {
                var result = MessageBox.Show("Сохранить изменения?", "Новый документ",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                    SaveFile();
            }

            EditorTextBox.Clear();
            isTextChanged = false;
        }

        private void MenuNew_Click(object sender, RoutedEventArgs e) => NewFile();

        private void OpenFile()
        {
            if (isTextChanged)
            {
                var ask = MessageBox.Show("Сохранить изменения?", "Открыть файл",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (ask == MessageBoxResult.Cancel)
                    return;

                if (ask == MessageBoxResult.Yes)
                    SaveFile();
            }

            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string text = File.ReadAllText(dialog.FileName);

                EditorTextBox.Text = text;
                undoStack.Clear();
                redoStack.Clear();
                undoStack.Push(text);

                currentFilePath = dialog.FileName;
                isTextChanged = false;

                this.Title = $"Compiler — {Path.GetFileName(currentFilePath)}";
            }
        }
        private void CheckFlexBison_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string parserPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "parser.exe");

                if (!File.Exists(parserPath))
                {
                    MessageBox.Show("parser.exe не найден в папке приложения.");
                    return;
                }

                var process = new Process();
                process.StartInfo.FileName = parserPath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.CreateNoWindow = true;

                process.Start();


                process.StandardInput.WriteLine(EditorTextBox.Text);
                process.StandardInput.Close();


                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                MessageBox.Show("Результат парсера:\n" + output);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }



        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
            UpdateStatusBar();
        }

        private void SaveFile()
        {
            if (currentFilePath == null)
            {
                SaveFileAs();
                return;
            }

            File.WriteAllText(currentFilePath, EditorTextBox.Text);
            isTextChanged = false;

            this.Title = $"Compiler — {Path.GetFileName(currentFilePath)}";
            UpdateStatusBar();
        }

        private void SaveFileAs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, EditorTextBox.Text);
                currentFilePath = dialog.FileName;
                isTextChanged = false;

                this.Title = $"Compiler — {Path.GetFileName(currentFilePath)}";
            }

            UpdateStatusBar();
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e) => SaveFile();
        private void MenuSaveAs_Click(object sender, RoutedEventArgs e) => SaveFileAs();



        private void MenuRun_Click(object sender, RoutedEventArgs e) => RunCompiler();

        private void RunCompiler()
        {
            string source = EditorTextBox.Text;

            try
            {
                var tokens = Scan(source);
                TokensGrid.ItemsSource = tokens;

                var lexErrors = LexicalValidator.Validate(tokens);

                var parser = new Parser(tokens);
                var ast = parser.ParseProgram();
                var parseErrors = parser.GetErrors();

                var lexAsSyntax = lexErrors.Select(le => new SyntaxError
                {
                    Line = le.Line,
                    Column = le.Column,
                    Message = le.Message,
                    Token = le.Token
                }).ToList();

                var lexKeys = new HashSet<(int Line, int Col)>(
                    lexAsSyntax.Select(e => (e.Line, e.Column)));
                var parseWithoutLexDup = parseErrors
                    .Where(pe => !lexKeys.Contains((pe.Line, pe.Column)))
                    .ToList();
                var allErrors = lexAsSyntax.Concat(parseWithoutLexDup).ToList();

                ErrorGrid.ItemsSource = allErrors;

                if (allErrors.Any())
                {
                    ShowSyntaxErrors(allErrors);

                    string astText = parser.PrintAst(ast);
                    MessageBox.Show($"Дерево разбора (с ошибками):\n\n{astText}",
                                  "Результат парсинга",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
                else
                {
                    string astText = parser.PrintAst(ast);
                    MessageBox.Show($"Синтаксических ошибок нет!\n\nДерево разбора:\n{astText}",
                                  "Успех",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }


        private void RunAntlrParser_Click(object sender, RoutedEventArgs e)
        {
            string source = EditorTextBox.Text;

            var (tree, errors) = AntlrRunner.Run(source);

            if (errors.Count > 0)
            {
                string msg = "Ошибки ANTLR:\n\n" + string.Join("\n", errors);
                MessageBox.Show(msg, "ANTLR — ошибки", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("ANTLR: ошибок нет!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            string pretty = PrettyTreePrinter.Print(tree, new KotlinConstParser(null));
            MessageBox.Show(pretty, "ANTLR AST");

        }


        private void ShowSyntaxErrors(List<SyntaxError> errors)
        {

            string message = " Найдены ошибки синтаксиса:\n\n";
            foreach (var error in errors)
            {
                message += $"Строка {error.Line}, позиция {error.Column}: {error.Message}\n";


                try
                {
                    int lineStart = EditorTextBox.GetCharacterIndexFromLineIndex(error.Line - 1);
                    int errorStart = lineStart + (error.Column - 1);
                    int errorLength = error.Token?.Lexeme.Length ?? 1;

                    EditorTextBox.Select(errorStart, errorLength);
                    EditorTextBox.Focus();
                }
                catch { }
            }


            MessageBox.Show(message, "Ошибки синтаксиса",
                           MessageBoxButton.OK,
                           MessageBoxImage.Warning);
        }

        private Token MakeErrorToken(int line, int col, string lexeme)
        {
            return new Token
            {
                Code = (int)TokenKind.Error,
                Type = "ошибка: недопустимый символ",
                Lexeme = lexeme,
                Line = line,
                Start = col,
                End = col,
                IsError = true
            };
        }


        private List<Token> Scan(string text)
        {
            var tokens = new List<Token>();

            int line = 1;
            int col = 1;
            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];


                if (c == '\r') { i++; continue; }
                if (c == '\n')
                {
                    line++;
                    col = 1;
                    i++;
                    continue;
                }


                if (c == ' ' || c == '\t')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Space,
                        Type = "разделитель (пробел)",
                        Lexeme = c == ' ' ? " " : "\\t",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++;
                    col++;
                    continue;
                }


                if (char.IsLetter(c) || c == '_')
                {
                    int startCol = col;
                    int start = i;

                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                        col++;
                    }

                    string lexeme = text.Substring(start, i - start);
                    bool isKeyword = Keywords.Contains(lexeme);

                    tokens.Add(new Token
                    {
                        Code = isKeyword ? (int)TokenKind.Keyword : (int)TokenKind.Identifier,
                        Type = isKeyword ? "ключевое слово" : "идентификатор",
                        Lexeme = lexeme,
                        Line = line,
                        Start = startCol,
                        End = col - 1
                    });

                    continue;
                }


                if (char.IsDigit(c) || c == '.')
                {
                    int startCol = col;
                    int start = i;

                    bool hasDot = false;
                    bool hasDigitsBeforeDot = false;
                    bool hasDigitsAfterDot = false;


                    if (char.IsDigit(c))
                    {
                        hasDigitsBeforeDot = true;
                        while (i < text.Length && char.IsDigit(text[i]))
                        {
                            i++;
                            col++;
                        }
                    }


                    if (i < text.Length && text[i] == '.')
                    {
                        hasDot = true;
                        i++;
                        col++;

                        if (i < text.Length && char.IsDigit(text[i]))
                        {
                            hasDigitsAfterDot = true;
                            while (i < text.Length && char.IsDigit(text[i]))
                            {
                                i++;
                                col++;
                            }
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Code = (int)TokenKind.Error,
                                Type = "ошибка: ожидается цифра после точки",
                                Lexeme = text.Substring(start, i - start),
                                Line = line,
                                Start = startCol,
                                End = col - 1,
                                IsError = true
                            });
                            continue;
                        }
                    }

                    string numberLexeme = text.Substring(start, i - start);

                    if (hasDot)
                    {
                        tokens.Add(new Token
                        {
                            Code = (int)TokenKind.RealNumber,
                            Type = "вещественное число",
                            Lexeme = numberLexeme,
                            Line = line,
                            Start = startCol,
                            End = col - 1
                        });
                    }
                    else
                    {
                        tokens.Add(new Token
                        {
                            Code = (int)TokenKind.UnsignedInteger,
                            Type = "целое без знака",
                            Lexeme = numberLexeme,
                            Line = line,
                            Start = startCol,
                            End = col - 1
                        });
                    }

                    continue;
                }


                if (c == '+')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Plus,
                        Type = "оператор сложения",
                        Lexeme = "+",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                if (c == '-')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Minus,
                        Type = "оператор вычитания",
                        Lexeme = "-",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                if (c == '*')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Multiply,
                        Type = "оператор умножения",
                        Lexeme = "*",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                if (c == '/')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Divide,
                        Type = "оператор деления",
                        Lexeme = "/",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                if (c == '=')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Assignment,
                        Type = "оператор присваивания",
                        Lexeme = "=",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                if (c == ';')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.StatementEnd,
                        Type = "конец оператора",
                        Lexeme = ";",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                if (c == ':')
                {
                    tokens.Add(new Token
                    {
                        Code = (int)TokenKind.Colon,
                        Type = "двоеточие",
                        Lexeme = ":",
                        Line = line,
                        Start = col,
                        End = col
                    });
                    i++; col++;
                    continue;
                }


                tokens.Add(MakeErrorToken(line, col, c.ToString()));
                i++;
                col++;
            }

            return tokens;
        }

        public class SearchResult
        {
            public string Value { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public int Length { get; set; }
        }

        private readonly Dictionary<string, string> SearchPatterns = new()
{
    { "Идентификатор", @"[A-Za-z$_][A-Za-z]*" },
    { "Пароль", @"[A-Za-zА-Яа-я0-9!@#$%^&*()_+={}

\[\]

:;""'<>,.?/\\|~`-]{10,}" },
    { "GUID", @"[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}" }
};

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTypeBox.SelectedItem is not ComboBoxItem item)
            {
                MessageBox.Show("Выберите тип поиска", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string type = item.Content.ToString();
            string text = EditorTextBox.Text;
            var results = new List<SearchResult>();

            if (string.IsNullOrWhiteSpace(text))
            {
                SearchResultsGrid.ItemsSource = null;
                SearchCountText.Text = "Найдено совпадений: 0";
                MessageBox.Show("Текст для поиска пуст", "Результат поиска",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (type == "Идентификатор")
                {
                    string pattern = @"(?<![A-Za-z0-9$_])[A-Za-z$_][A-Za-z]*(?![A-Za-z0-9$_])";
                    var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern, RegexOptions.Multiline);

                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        int lineNumber = GetLineNumberFromIndex(text, m.Index);
                        int columnNumber = GetColumnNumberFromIndex(text, m.Index, lineNumber);

                        results.Add(new SearchResult
                        {
                            Value = m.Value,
                            Line = lineNumber,
                            Column = columnNumber,
                            Length = m.Length
                        });
                    }
                }
                else if (type == "Пароль")
                {
                    string pattern = @"^[A-Za-zА-Яа-я0-9!@#$%^&*()_+={}\[\]\:;""'<>,.?/\\|~` -]{10,}$";
                    string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        if (line.Length >= 10)
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
                            if (match.Success)
                            {
                                int index = text.IndexOf(line, StringComparison.Ordinal);
                                int lineNumber = GetLineNumberFromIndex(text, index);
                                int columnNumber = GetColumnNumberFromIndex(text, index, lineNumber);

                                results.Add(new SearchResult
                                {
                                    Value = line,
                                    Line = lineNumber,
                                    Column = columnNumber,
                                    Length = line.Length
                                });
                            }
                        }
                    }
                }
                else if (type == "GUID (Regex)")
                {
                    // Используем Regex для поиска GUID
                    string pattern = @"\b[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\b";
                    var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern, RegexOptions.Multiline);

                    foreach (System.Text.RegularExpressions.Match m in matches)
                    {
                        int lineNumber = GetLineNumberFromIndex(text, m.Index);
                        int columnNumber = GetColumnNumberFromIndex(text, m.Index, lineNumber);

                        results.Add(new SearchResult
                        {
                            Value = m.Value,
                            Line = lineNumber,
                            Column = columnNumber,
                            Length = m.Length
                        });
                    }
                }
                else if (type == "GUID (Автомат)")
                {
                    // Используем конечный автомат для поиска GUID
                    var guidResults = GUIDAutomaton.FindAll(text);

                    foreach (var guid in guidResults)
                    {
                        results.Add(new SearchResult
                        {
                            Value = guid.value,
                            Line = guid.line,
                            Column = guid.column,
                            Length = guid.value.Length
                        });
                    }
                }
            }
            catch (System.Text.RegularExpressions.RegexParseException ex)
            {
                MessageBox.Show($"Ошибка в регулярном выражении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SearchResultsGrid.ItemsSource = results;

            if (results.Count > 0)
            {
                SearchCountText.Text = $"Найдено совпадений: {results.Count}";
                MessageBox.Show($"Найдено {results.Count} совпадений для типа \"{type}\"",
                    "Результат поиска", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                SearchCountText.Text = "Ничего не найдено";
                MessageBox.Show($"Ничего не найдено для типа \"{type}\"",
                    "Результат поиска", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private int GetLineNumberFromIndex(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    line++;
            }
            return line;
        }

        private int GetColumnNumberFromIndex(string text, int index, int lineNumber)
        {
            int lineStart = 0;
            int currentLine = 1;

            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    currentLine++;
                    lineStart = i + 1;
                }
            }

            return index - lineStart + 1;
        }

        public class GUIDAutomaton
        {
            private enum State
            {
                S0, S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11, S12, S13, S14,
                S15, S16, S17, S18, S19, S20, S21, S22, S23, S24, S25, S26, S27,
                S28, S29, S30, S31, S32, S33, S34, S35, S36, Error
            }

            private static bool IsHexChar(char c)
            {
                return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
            }

            private static State NextState(State state, char c)
            {
                if (state == State.Error) return State.Error;

                // Состояния S0-S8 (первые 8 hex символов)
                if (state >= State.S0 && state <= State.S7 && IsHexChar(c))
                    return state + 1;

                if (state == State.S8 && c == '-')
                    return State.S9;

                // Состояния S9-S13 (следующие 4 hex)
                if (state >= State.S9 && state <= State.S12 && IsHexChar(c))
                    return state + 1;

                if (state == State.S13 && c == '-')
                    return State.S14;

                // Состояния S14-S18 (следующие 4 hex)
                if (state >= State.S14 && state <= State.S17 && IsHexChar(c))
                    return state + 1;

                if (state == State.S18 && c == '-')
                    return State.S19;

                // Состояния S19-S23 (следующие 4 hex)
                if (state >= State.S19 && state <= State.S22 && IsHexChar(c))
                    return state + 1;

                if (state == State.S23 && c == '-')
                    return State.S24;

                // Состояния S24-S36 (последние 12 hex)
                if (state >= State.S24 && state <= State.S35 && IsHexChar(c))
                    return state + 1;

                return State.Error;
            }

            public static List<(int start, int end, string value, int line, int column)> FindAll(string text)
            {
                var results = new List<(int start, int end, string value, int line, int column)>();
                State currentState = State.S0;
                int startIndex = -1;
                int currentLine = 1;
                int currentColumn = 1;

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    // Обновляем позицию
                    if (c == '\n')
                    {
                        currentLine++;
                        currentColumn = 1;
                    }
                    else
                    {
                        currentColumn++;
                    }

                    if (currentState == State.S0 && IsHexChar(c))
                    {
                        startIndex = i;
                        currentState = State.S1;
                    }
                    else
                    {
                        State next = NextState(currentState, c);

                        if (next != State.Error)
                        {
                            currentState = next;
                        }
                        else
                        {
                            // Если достигли терминального состояния, сохраняем GUID
                            if (currentState == State.S36 && startIndex >= 0)
                            {
                                string guid = text.Substring(startIndex, i - startIndex);

                                int guidLine = GetLineNumber(text, startIndex);
                                int guidColumn = GetColumnNumber(text, startIndex, guidLine);

                                results.Add((startIndex, i - 1, guid, guidLine, guidColumn));
                            }

                            // Сброс и попытка начать заново с текущего символа
                            if (IsHexChar(c))
                            {
                                startIndex = i;
                                currentState = State.S1;
                            }
                            else
                            {
                                currentState = State.S0;
                                startIndex = -1;
                            }
                        }
                    }
                }

                // Проверка в конце строки
                if (currentState == State.S36 && startIndex >= 0)
                {
                    string guid = text.Substring(startIndex, text.Length - startIndex);
                    int guidLine = GetLineNumber(text, startIndex);
                    int guidColumn = GetColumnNumber(text, startIndex, guidLine);
                    results.Add((startIndex, text.Length - 1, guid, guidLine, guidColumn));
                }

                return results;
            }

            private static int GetLineNumber(string text, int index)
            {
                int line = 1;
                for (int i = 0; i < index && i < text.Length; i++)
                {
                    if (text[i] == '\n')
                        line++;
                }
                return line;
            }


            private static int GetColumnNumber(string text, int index, int lineNumber)
            {
                int lineStart = 0;
                int currentLine = 1;

                for (int i = 0; i < index && i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        currentLine++;
                        lineStart = i + 1;
                    }
                }

                return index - lineStart + 1;
            }
        }






        private void TokensGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TokensGrid.SelectedItem is Token token)
            {
                int line = token.Line;
                int col = token.Start;

                int lineStartIndex = EditorTextBox.GetCharacterIndexFromLineIndex(line - 1);
                int caretIndex = lineStartIndex + (col - 1);

                EditorTextBox.Focus();
                EditorTextBox.CaretIndex = caretIndex;
                EditorTextBox.ScrollToLine(line - 1);
            }
        }



        private void Undo()
        {
            if (undoStack.Count > 1)
            {
                isInternalChange = true;
                redoStack.Push(undoStack.Pop());
                EditorTextBox.Text = undoStack.Peek();
                isInternalChange = false;
            }
        }
        private void SearchResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsGrid.SelectedItem is SearchResult result)
            {
                int lineStart = EditorTextBox.GetCharacterIndexFromLineIndex(result.Line - 1);
                int start = lineStart + (result.Column - 1);

                EditorTextBox.Focus();
                EditorTextBox.Select(start, result.Length);
                EditorTextBox.ScrollToLine(result.Line - 1);
            }
        }
        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                isInternalChange = true;
                string state = redoStack.Pop();
                undoStack.Push(state);
                EditorTextBox.Text = state;
                isInternalChange = false;
            }
        }



        private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Redo();



        private void Copy_Click(object sender, RoutedEventArgs e) => EditorTextBox.Copy();
        private void Cut_Click(object sender, RoutedEventArgs e) => EditorTextBox.Cut();
        private void Paste_Click(object sender, RoutedEventArgs e) => EditorTextBox.Paste();



        private void MenuAbout_Click(object sender, RoutedEventArgs e) => new AboutWindow().ShowDialog();
        private void MenuHelp_Click(object sender, RoutedEventArgs e) => new HelpWindow().ShowDialog();



        private void ApplyFontSize(double size)
        {
            if (EditorTextBox == null)
                return;

            if (size < 8) size = 8;
            if (size > 72) size = 72;

            EditorTextBox.FontSize = size;

            if (FontSizeBox != null)
                FontSizeBox.Text = size.ToString();

            UpdateStatusBar();
        }

        private void FontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(FontSizeBox.Text, out double size))
                ApplyFontSize(size + 1);
        }

        private void FontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(FontSizeBox.Text, out double size))
                ApplyFontSize(size - 1);
        }

        private void FontSizeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(FontSizeBox.Text, out double size))
                ApplyFontSize(size);
        }



        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            if (isTextChanged)
            {
                var ask = MessageBox.Show(
                    "Сохранить изменения перед выходом?",
                    "Выход",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (ask == MessageBoxResult.Cancel)
                    return;

                if (ask == MessageBoxResult.Yes)
                    SaveFile();
            }

            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isTextChanged)
            {
                var ask = MessageBox.Show(
                    "Сохранить изменения перед выходом?",
                    "Выход",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (ask == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (ask == MessageBoxResult.Yes)
                    SaveFile();
            }
        }



        private void UpdateLineNumbers()
        {
            int lineCount = EditorTextBox.LineCount;
            var sb = new StringBuilder();

            for (int i = 1; i <= lineCount; i++)
                sb.AppendLine(i.ToString());

            LineNumbers.Text = sb.ToString();
        }



        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
            UpdateStatusBar();
        }

        private void EditorTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            LineNumbersScroll.ScrollToVerticalOffset(e.VerticalOffset);
        }



        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            string path = files[0];
            string text = File.ReadAllText(path);

            EditorTextBox.Text = text;
            currentFilePath = path;

            undoStack.Clear();
            redoStack.Clear();
            undoStack.Push(text);
            isTextChanged = false;

            this.Title = $"Compiler — {Path.GetFileName(path)}";
        }



        private void UpdateStatusBar()
        {
            int line = EditorTextBox.GetLineIndexFromCharacterIndex(EditorTextBox.CaretIndex) + 1;
            int column = EditorTextBox.CaretIndex - EditorTextBox.GetCharacterIndexFromLineIndex(line - 1) + 1;

            string fileName = currentFilePath != null
                ? Path.GetFileName(currentFilePath)
                : "Без имени";

            string modified = isTextChanged ? "Изменён" : "Сохранён";

            StatusBarText.Text =
                $"Строка: {line}, Столбец: {column} | Строк: {EditorTextBox.LineCount} | Файл: {fileName} | {modified}";
        }
    }
}
