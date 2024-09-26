using HelixSharpDemo.ViewModel;
using System.Windows.Controls;

namespace HelixSharpDemo.View
{
    /// <summary>
    /// MeshGeometry3DView.xaml 的交互逻辑
    /// </summary>
    public partial class MeshGeometry3DView : UserControl
    {
        public MeshGeometry3DView()
        {
            this.DataContext = new MeshGeometry3DViewModel();

            InitializeComponent();
        }
    }
}
