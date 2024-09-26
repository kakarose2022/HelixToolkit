using HelixSharpDemo.ViewModel;
using System.Windows.Controls;

namespace HelixSharpDemo.View
{
    /// <summary>
    /// DemoView.xaml 的交互逻辑
    /// </summary>
    public partial class ModelVisual3DView : UserControl
    {
        public ModelVisual3DView()
        {
            this.DataContext = new ModelVisual3DViewModel();
            InitializeComponent();
        }
    }
}
