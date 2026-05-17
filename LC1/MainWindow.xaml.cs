using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LC1.Core;
using LC1.Lab7;

namespace LC1
{
    public partial class MainWindow : Window
    {
        private bool isTextChanged = false;
        private string? currentFilePath = null;

        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private bool isInternalChange = false;

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
                var scanResult = Scanner.Analyze(source);
                var lexemes = scanResult.Lexemes;

                var tokens = lexemes.Select(l => new
                {
                    Code = (int)l.Code,
                    Type = l.Type,
                    Lexeme = l.Text,
                    Line = l.Line,
                    Start = l.StartColumn,
                    End = l.EndColumn,
                    Location = $"строка {l.Line}, {l.StartColumn}-{l.EndColumn}"
                }).ToList();

                TokensGrid.ItemsSource = tokens;

                var parseResult = LC1.Core.Parser.Analyze(source);
                var errors = parseResult.Errors;

                var syntaxErrors = errors.Select(e => new
                {
                    Fragment = string.IsNullOrEmpty(e.Fragment) ? "(пусто)" : e.Fragment,
                    Location = $"строка {e.Line}, {e.StartColumn}-{e.EndColumn}",
                    Message = e.Message,
                    Line = e.Line,
                    StartColumn = e.StartColumn,
                    EndColumn = e.EndColumn
                }).ToList();

                ErrorGrid.ItemsSource = syntaxErrors;

                if (errors.Any())
                {
                    StatusBarText.Text = $"Найдено ошибок: {errors.Count}";
                    HighlightErrorInEditor(errors.First());
                }
                else
                {
                    StatusBarText.Text = "Синтаксических ошибок нет";
                }
            }
            catch (Exception ex)
            {
                StatusBarText.Text = "Ошибка при анализе";
                MessageBox.Show($"Ошибка при анализе: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void HighlightErrorInEditor(ParseError error)
        {
            try
            {
                int lineStart = EditorTextBox.GetCharacterIndexFromLineIndex(error.Line - 1);
                int errorStart = lineStart + (error.StartColumn - 1);
                int errorLength = error.Fragment?.Length ?? 1;

                EditorTextBox.Focus();
                EditorTextBox.Select(errorStart, errorLength);
                EditorTextBox.ScrollToLine(error.Line - 1);
            }
            catch { }
        }

        private void ErrorGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic error = ErrorGrid.SelectedItem;
            if (error != null)
            {
                int line = error.Line;
                int startCol = error.StartColumn;
                int endCol = error.EndColumn;

                int lineStart = EditorTextBox.GetCharacterIndexFromLineIndex(line - 1);
                int start = lineStart + (startCol - 1);
                int length = endCol - startCol + 1;

                EditorTextBox.Focus();
                EditorTextBox.Select(start, length);
                EditorTextBox.ScrollToLine(line - 1);
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

        private void RunLab7_Click(object sender, RoutedEventArgs e) => RunLab7Analysis();

        private void RunLab7Analysis()
        {
            var result = Lab7Analyzer.Analyze(EditorTextBox.Text);
            Lab7OutputTextBox.Text = Lab7Analyzer.FormatReport(result);
            BottomTabControl.SelectedIndex = 2;

            if (result.Success)
                StatusBarText.Text = "ЛР7: AST и IR построены";
            else
                StatusBarText.Text = "ЛР7: ошибка анализа";
        }

        private void TokensGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic token = TokensGrid.SelectedItem;
            if (token != null)
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