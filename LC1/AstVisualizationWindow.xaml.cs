using System.Windows;
using LC1.Ast;

namespace LC1
{
    public partial class AstVisualizationWindow : Window
    {
        public AstVisualizationWindow(ProgramNode program)
        {
            InitializeComponent();
            AstGraphRenderer.Render(AstCanvas, program);
        }
    }
}
