using System;
using System.IO;
using System.Windows;

namespace LC1
{
    public partial class TestExamplesWindow : Window
    {
        public TestExamplesWindow()
        {
            InitializeComponent();

            string html;
            using (var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/TestExamples.html")).Stream)
            using (var reader = new StreamReader(stream))
                html = reader.ReadToEnd();

            for (int fig = 2; fig <= 7; fig++)
            {
                var packUri = new Uri($"pack://application:,,,/Resources/TestExampleFig{fig:D2}.png");
                var tempPng = Path.Combine(Path.GetTempPath(), $"LC1_TestExampleFig{fig:D2}.png");
                using (var pngStream = Application.GetResourceStream(packUri).Stream)
                using (var fs = File.Create(tempPng))
                    pngStream.CopyTo(fs);

                html = html.Replace($"___FIG{fig:D2}___", new Uri(tempPng).AbsoluteUri);
            }

            Browser.NavigateToString(html);
        }
    }
}
