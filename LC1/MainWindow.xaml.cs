using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using System.IO;


namespace LC1
{
    public partial class MainWindow : Window
    {
        private bool isTextChanged = false;
        public MainWindow()
        {
            InitializeComponent();
            ApplyFontSize(14);
            EditorTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(EditorTextBox_ScrollChanged));

            EditorTextBox.TextChanged += (s, e) =>
            {
                isTextChanged = true;
            };

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

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.New, (s, e) => NewFile()));

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Open, (s, e) => OpenFile()));

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Save, (s, e) => SaveFile()));

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.SaveAs, (s, e) => SaveFileAs()));

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Undo, (s, e) => Undo()));

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Redo, (s, e) => Redo()));

            CommandBindings.Add(new CommandBinding(
                NavigationCommands.Refresh, (s, e) => RunCompiler()));

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Close, (s, e) => MenuExit_Click(s, e)));


        }

        private void NewFile()
        {
            undoStack.Clear();
            redoStack.Clear();
            undoStack.Push("");

            if (isTextChanged)
            {
                var result = MessageBox.Show("Сохранить изменения?", "Новый документ", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile();
                }
            }
            EditorTextBox.Clear();
            OutputTextBox.Clear();
            isTextChanged = false;
        }
        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            NewFile();
        }

        private string? currentFilePath = null;

        private void OpenFile()
        {
            if (isTextChanged)
            {
                var ask = MessageBox.Show("Сохранить изменения?", "Открыть файл", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (ask == MessageBoxResult.Yes)
                {
                    SaveFile();
                }
                if (ask == MessageBoxResult.Cancel)
                {
                    return;
                }
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

                this.Title = $"Compiler — {System.IO.Path.GetFileName(currentFilePath)}";
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

            this.Title = $"Compiler — {System.IO.Path.GetFileName(currentFilePath)}";
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

                this.Title = $"Compiler — {System.IO.Path.GetFileName(currentFilePath)}";
            }
            UpdateStatusBar();

        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileAs();
        }

        public class Compiler
        {
            public string Run(string source)
            {
                if (string.IsNullOrWhiteSpace(source))
                    return "Строка пуста.";

                return source;
            }
        }

        private void MenuRun_Click(object sender, RoutedEventArgs e)
        {
            RunCompiler();
        }

        private void RunCompiler()
        {
            string source = EditorTextBox.Text;

            try
            {
                var compiler = new Compiler();
                string result = compiler.Run(source);

                OutputTextBox.Text = result;
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = "Ошибка выполнения:\n" + ex.Message;
            }
        }

        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private bool isInternalChange = false;

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
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Copy();
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Cut();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Paste();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            new HelpWindow().ShowDialog();
        }

        private void ApplyFontSize(double size)
        {
            if (EditorTextBox == null || OutputTextBox == null)
                return;

            if (size < 8) size = 8;
            if (size > 72) size = 72;

            EditorTextBox.FontSize = size;
            OutputTextBox.FontSize = size;

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
                {
                    SaveFile();
                }
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

            this.Title = $"Compiler — {System.IO.Path.GetFileName(path)}";
        }
        private void UpdateStatusBar()
        {
            int line = EditorTextBox.GetLineIndexFromCharacterIndex(EditorTextBox.CaretIndex) + 1;
            int column = EditorTextBox.CaretIndex - EditorTextBox.GetCharacterIndexFromLineIndex(line - 1) + 1;

            string fileName = currentFilePath != null
                ? System.IO.Path.GetFileName(currentFilePath)
                : "Без имени";

            string modified = isTextChanged ? "Изменён" : "Сохранён";

            StatusBarText.Text = $"Строка: {line}, Столбец: {column} | Строк: {EditorTextBox.LineCount} | Файл: {fileName} | {modified}";
        }

    }
}
