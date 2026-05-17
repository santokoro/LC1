using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LC1.Core;

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

            if (string.IsNullOrWhiteSpace(EditorTextBox.Text))
                EditorTextBox.Text = "6 + 7 + 10 * 4";

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
                var scanResult = ExprScanner.Analyze(source);
                var tokens = scanResult.Tokens
                    .Where(t => t.Kind != ExprTokenKind.End)
                    .Select(t => new
                    {
                        Code = (int)t.Kind,
                        Type = t.Type,
                        Lexeme = string.IsNullOrEmpty(t.Text) ? "⟨конец⟩" : t.Text,
                        Line = t.Line,
                        Start = t.StartColumn,
                        End = t.EndColumn,
                        Location = $"строка {t.Line}, {t.StartColumn}-{t.EndColumn}"
                    })
                    .ToList();

                TokensGrid.ItemsSource = tokens;

                var parseResult = ExprRecursiveDescentParser.AnalyzeTokens(scanResult.Tokens);

                var lexicalErrors = scanResult.Tokens
                    .Where(t => t.Kind == ExprTokenKind.Error)
                    .Select(t => new ExprParseError
                    {
                        Fragment = t.Text,
                        Message = "недопустимая лексема",
                        Line = t.Line,
                        StartColumn = t.StartColumn,
                        EndColumn = t.EndColumn
                    });

                var allErrors = lexicalErrors.Concat(parseResult.Errors).ToList();
                int totalErrors = allErrors.Count;

                var errorRows = allErrors.Select(e => new
                {
                    Fragment = string.IsNullOrEmpty(e.Fragment) ? "(пусто)" : e.Fragment,
                    Location = $"строка {e.Line}, {e.StartColumn}-{e.EndColumn}",
                    Message = e.Message,
                    Line = e.Line,
                    StartColumn = e.StartColumn,
                    EndColumn = e.EndColumn
                }).ToList();

                if (totalErrors == 0)
                {
                    ErrorsOkText.Visibility = Visibility.Visible;
                    ErrorGrid.Visibility = Visibility.Collapsed;
                    ErrorGrid.ItemsSource = null;
                }
                else
                {
                    ErrorsOkText.Visibility = Visibility.Collapsed;
                    ErrorGrid.Visibility = Visibility.Visible;
                    ErrorGrid.ItemsSource = errorRows;
                }

                if (parseResult.IsSuccess)
                {
                    TetradsPlaceholderText.Visibility = Visibility.Collapsed;
                    TetradsGrid.Visibility = Visibility.Visible;
                    TetradsGrid.ItemsSource = parseResult.Tetrads;

                    RpnTextBox.Text = parseResult.RpnText;
                    if (parseResult.CanEvaluate && parseResult.EvaluatedValue.HasValue)
                        EvalResultText.Text = parseResult.EvaluatedValue.Value.ToString();
                    else
                        EvalResultText.Text = parseResult.EvaluationMessage ?? "—";
                }
                else
                {
                    TetradsPlaceholderText.Visibility = Visibility.Visible;
                    TetradsGrid.Visibility = Visibility.Collapsed;
                    TetradsGrid.ItemsSource = null;
                    RpnTextBox.Text = "";
                    EvalResultText.Text = "—";
                }

                if (allErrors.Count > 0)
                {
                    StatusBarText.Text = $"Найдено ошибок: {totalErrors}";
                    HighlightErrorInEditor(allErrors[0]);
                }
                else
                {
                    StatusBarText.Text = parseResult.CanEvaluate && parseResult.EvaluatedValue.HasValue
                        ? $"Ошибок нет. ПОЛИЗ: {parseResult.RpnText} = {parseResult.EvaluatedValue}"
                        : $"Ошибок нет. ПОЛИЗ: {parseResult.RpnText}";
                }
            }
            catch (Exception ex)
            {
                StatusBarText.Text = "Ошибка при анализе";
                ErrorsOkText.Visibility = Visibility.Collapsed;
                ErrorGrid.Visibility = Visibility.Visible;
                MessageBox.Show($"Ошибка при анализе: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void HighlightErrorInEditor(ExprParseError error)
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