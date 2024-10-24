using HelixSharpDemo.ViewModel;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

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


        public void Test(List<LineGeometry3D> lineGeometry3Ds)
        {
            aaaa.Children.Clear();
            foreach (var item in lineGeometry3Ds)
            {
                LineGeometryModel3D lineGeometryModel3D = new LineGeometryModel3D()
                {
                    Geometry = item,
                    Color = System.Windows.Media.Colors.Red
                };

                aaaa.Children.Add(lineGeometryModel3D);
            }
          

        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is MeshGeometry3DViewModel vm)
            {
                vm.ChangePostionByUi -= Test;
                vm.ChangePostionByUi += Test;
            }
        }
    }
}
