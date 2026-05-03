using System;
using System.IO;
using System.Windows;

namespace LC1
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            var uri = new Uri("pack://application:,,,/Resources/About.html");
            var stream = Application.GetResourceStream(uri).Stream;

            using var reader = new StreamReader(stream);
            string html = reader.ReadToEnd();

            Browser.NavigateToString(html);
        }

    }
}
