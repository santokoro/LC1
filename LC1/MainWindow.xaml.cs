using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LC1
{
    // ------------------ МОДЕЛИ ЛЕКСЕМ ------------------

    public enum TokenKind
    {
        UnsignedInteger = 1,      // целое без знака
        Identifier = 2,           // идентификатор
        RealNumber = 3,           // вещественное число

        Assignment = 10,          // =
        Space = 11,               // пробел
        Keyword = 14,             // ключевое слово
        StatementEnd = 16,        // ;
        Colon = 17,               // :

        Plus = 20,                // +
        Minus = 21,               // -
        Multiply = 22,            // *
        Divide = 23,              // /

        Error = 99                // ошибка
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

    // ------------------ ОСНОВНОЕ ОКНО ------------------

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

        // ------------------ ФАЙЛОВЫЕ ОПЕРАЦИИ ------------------

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

                // Передаём текст в парсер
                process.StandardInput.WriteLine(EditorTextBox.Text);
                process.StandardInput.Close();

                // Читаем вывод
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

        // ------------------ ЛЕКСИЧЕСКИЙ АНАЛИЗ ------------------

        private void MenuRun_Click(object sender, RoutedEventArgs e) => RunCompiler();

        private void RunCompiler()
        {
            string source = EditorTextBox.Text;

            try
            {
                // 1. Лексический анализ
                var tokens = Scan(source);
                TokensGrid.ItemsSource = tokens;

                // 2. Синтаксический анализ
                var parser = new Parser(tokens);
                var ast = parser.ParseProgram();
                var errors = parser.GetErrors();

                // 3. Показываем результат
                if (errors.Any())
                {
                    ShowSyntaxErrors(errors);

                    // Всё равно показываем дерево (может быть частичным)
                    string astText = parser.PrintAst(ast);
                    MessageBox.Show($"Дерево разбора (с ошибками):\n\n{astText}",
                                  "Результат парсинга",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
                else
                {
                    // Всё хорошо - показываем дерево
                    string astText = parser.PrintAst(ast);
                    MessageBox.Show($"✅ Синтаксических ошибок нет!\n\nДерево разбора:\n{astText}",
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

        private void ShowSyntaxErrors(List<SyntaxError> errors)
        {
            // Формируем сообщение об ошибках
            string message = "❌ Найдены ошибки синтаксиса:\n\n";
            foreach (var error in errors)
            {
                message += $"Строка {error.Line}, позиция {error.Column}: {error.Message}\n";

                // Подсвечиваем ошибку в тексте
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

            // Показываем предупреждение
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

                // Перевод строки
                if (c == '\r') { i++; continue; }
                if (c == '\n')
                {
                    line++;
                    col = 1;
                    i++;
                    continue;
                }

                // Пробел / таб
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

                // Идентификатор / ключевое слово
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

                // ------------------ ЧИСЛО ------------------
                // Вариант B: знак НЕ является частью числа
                if (char.IsDigit(c) || c == '.')
                {
                    int startCol = col;
                    int start = i;

                    bool hasDot = false;
                    bool hasDigitsBeforeDot = false;
                    bool hasDigitsAfterDot = false;

                    // целая часть
                    if (char.IsDigit(c))
                    {
                        hasDigitsBeforeDot = true;
                        while (i < text.Length && char.IsDigit(text[i]))
                        {
                            i++;
                            col++;
                        }
                    }

                    // точка
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

                // ------------------ ОПЕРАТОРЫ ------------------

                // +
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

                // -
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

                // *
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

                // /
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

                // ------------------ СЛУЖЕБНЫЕ СИМВОЛЫ ------------------

                // =
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

                // ;
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

                // :
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

                // ------------------ ОШИБКА ------------------
                tokens.Add(MakeErrorToken(line, col, c.ToString()));
                i++;
                col++;
            }

            return tokens;
        }

        // ------------------ ПЕРЕХОД К ОШИБКЕ ------------------

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

        // ------------------ ОТМЕНА / ПОВТОР ------------------

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

        // ------------------ КОПИРОВАНИЕ / ВСТАВКА ------------------

        private void Copy_Click(object sender, RoutedEventArgs e) => EditorTextBox.Copy();
        private void Cut_Click(object sender, RoutedEventArgs e) => EditorTextBox.Cut();
        private void Paste_Click(object sender, RoutedEventArgs e) => EditorTextBox.Paste();

        // ------------------ СПРАВКА / О ПРОГРАММЕ ------------------

        private void MenuAbout_Click(object sender, RoutedEventArgs e) => new AboutWindow().ShowDialog();
        private void MenuHelp_Click(object sender, RoutedEventArgs e) => new HelpWindow().ShowDialog();

        // ------------------ РАЗМЕР ШРИФТА ------------------

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

        // ------------------ ВЫХОД ------------------

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

        // ------------------ НОМЕРА СТРОК ------------------

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

        // ------------------ DRAG & DROP ------------------

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

        // ------------------ СТАТУС-БАР ------------------

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

