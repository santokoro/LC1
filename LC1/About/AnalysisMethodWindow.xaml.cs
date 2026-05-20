using System;
using System.IO;
using System.Windows;

namespace LC1
{
    public partial class AnalysisMethodWindow : Window
    {
        public AnalysisMethodWindow()
        {
            InitializeComponent();

            string html;
            using (var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/AnalysisMethod.html")).Stream)
            using (var reader = new StreamReader(stream))
                html = reader.ReadToEnd();

            var tempPng = Path.Combine(Path.GetTempPath(), "LC1_FiniteAutomaton.png");
            using (var pngStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/FiniteAutomaton.png")).Stream)
            using (var fs = File.Create(tempPng))
                pngStream.CopyTo(fs);

            var imgUri = new Uri(tempPng).AbsoluteUri;
            html = html.Replace("___AUTOMATON_SRC___", imgUri);

            Browser.NavigateToString(html);
        }
    }
}
