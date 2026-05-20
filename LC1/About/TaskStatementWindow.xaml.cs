using System.IO;
using System.Windows;

namespace LC1
{
    public partial class TaskStatementWindow : Window
    {
        public TaskStatementWindow()
        {
            InitializeComponent();

            var uri = new System.Uri("pack://application:,,,/Resources/TaskStatement.html");
            var stream = Application.GetResourceStream(uri).Stream;

            using var reader = new StreamReader(stream);
            string html = reader.ReadToEnd();

            Browser.NavigateToString(html);
        }
    }
}
