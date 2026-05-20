using System.Collections.Generic;
using System.IO;
using System.Windows;
using LC1.SourceView;

namespace LC1
{
    public partial class SourceCodeWindow : Window
    {
        public SourceCodeWindow()
        {
            InitializeComponent();

            var projectDir = SourceCodeFormatter.FindLc1ProjectDirectory();
            if (projectDir == null)
            {
                Browser.NavigateToString(
                    "<html><body><p>Не удалось найти каталог проекта LC1 (LC1.csproj).</p></body></html>");
                return;
            }

            var files = new (string Title, string RelativePath, bool IsXaml)[]
            {
                ("Core/Parser.cs", Path.Combine("Core", "Parser.cs"), false),
                ("MainWindow.xaml.cs", "MainWindow.xaml.cs", false),
                ("MainWindow.xaml", "MainWindow.xaml", true),
                ("Core/Scanner.cs", Path.Combine("Core", "Scanner.cs"), false),
                ("Core/ScannerModel.cs", Path.Combine("Core", "ScannerModel.cs"), false),
            };

            var sections = new List<(string Title, string Code)>();
            foreach (var (title, relativePath, isXaml) in files)
            {
                var fullPath = Path.Combine(projectDir, relativePath);
                if (!File.Exists(fullPath))
                    continue;

                var text = File.ReadAllText(fullPath);
                if (isXaml)
                    text = SourceCodeFormatter.RemoveXmlComments(text);
                else
                    text = SourceCodeFormatter.RemoveCSharpComments(text);

                sections.Add((title, text.TrimEnd()));
            }

            var html = SourceCodeFormatter.ToHtmlDocument(sections);
            Browser.NavigateToString(html);
        }
    }
}
