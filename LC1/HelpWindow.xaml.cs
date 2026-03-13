using System;
using System.IO;
using System.Windows;

namespace LC1
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();

            var uri = new Uri("pack://application:,,,/Resources/Help.html");
            var stream = Application.GetResourceStream(uri).Stream;

            using var reader = new StreamReader(stream);
            string html = reader.ReadToEnd();

            Browser.NavigateToString(html);
        }


    }
}
