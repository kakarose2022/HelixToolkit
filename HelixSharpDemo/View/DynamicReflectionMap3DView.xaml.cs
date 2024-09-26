using HelixSharpDemo.ViewModel;
using SharpDX.Direct2D1;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.SharpDX.Core.Model.Scene;
using SharpDX.Direct2D1.Effects;
using HelixToolkit.SharpDX.Core;
using System.Windows.Markup;

namespace HelixSharpDemo.View
{
    /// <summary>
    /// DynamicReflectionMap3DView.xaml 的交互逻辑
    /// </summary>
    public partial class DynamicReflectionMap3DView : UserControl
    {
        public DynamicReflectionMap3DView()
        {
            InitializeComponent();
            this.DataContext = new DynamicReflectionMap3DViewModel();
            view1.AddHandler(Element3D.MouseDown3DEvent, new RoutedEventHandler((s, e) =>
            {
                var arg = e as MouseDown3DEventArgs;

                if (arg.HitTestResult == null)
                {
                    return;
                }
                var aaa =  view1.CursorPosition;

               // view1.UnProject(view1.CursorPosition);

                //if (arg.HitTestResult.ModelHit is SceneNode node && node.Tag is AttachedNodeViewModel vm)
                if (arg.HitTestResult.ModelHit is MeshGeometryModel3D geometry)
                {
                   // MessageBox.Show(@$"x: {arg.Position.X}  Y: {arg.Position.Y}");
                }
            }));

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                vm.ChangeDyContent -= AttatchToDynamic;
                vm.ChangeDyContent += AttatchToDynamic;
                vm.Reload();
            }
        }

        public void AttatchToDynamic()
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                dynamic.Children.Clear();
                foreach (var sceneNode in vm.SceneNodes)
                {
                    foreach (var meshGeometryModel3D in vm.SceneNodeToMeshGeometry3D(sceneNode.Root))
                    {
                        dynamic.Children.Add(meshGeometryModel3D);
                    }
                }         
            }
        }

        private void view1_MouseDown3D(object sender, RoutedEventArgs e)
        {

        }

        private void view1_MouseUp3D(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                var ev = e as MouseUp3DEventArgs;
                if (ev != null && !ev.Handled)
                {
                    vm.SelectObject = ev.HitTestResult?.ModelHit;
                }
            }
        }
    }
}
