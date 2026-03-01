using System.Windows;

namespace LC1
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "About.html");

            Browser.Navigate(new Uri(path));
        }
    }
}

