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

            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Help.html");

            Browser.Navigate(new Uri(path));
        }
    }
}
