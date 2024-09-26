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
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using System.Diagnostics;

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
                vm.MeshGeometryModel3Ds.Clear();
                dynamic.Children.Clear();
                foreach (var sceneNode in vm.SceneNodes)
                {
                    var oneSceneMeshGeometry3Ds = vm.SceneNodeToMeshGeometry3D(sceneNode.Root);
                    foreach (var meshGeometryModel3D in oneSceneMeshGeometry3Ds)
                    {
                        dynamic.Children.Add(meshGeometryModel3D);
                        vm.MeshGeometryModel3Ds.Add(meshGeometryModel3D);
                    }
                }         
            }
        }

        //private void view1_MouseDown3D(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext is DynamicReflectionMap3DViewModel vm)
        //    {
        //        var ev = e as MouseDown3DEventArgs ;
        //        if (ev != null && !ev.Handled)
        //        {
        //            if (ev.HitTestResult != null
        //                && ev.HitTestResult.ModelHit is MeshGeometryModel3D m)
        //            {
        
        //                Trace.WriteLine("MouseDown....");



        //                vm.Target = null;
        //                vm.CenterOffset = m.Geometry.Bound.Center; // Must update this before updating target
        //                vm.Target = ev.HitTestResult.ModelHit as Element3D;
        //                vm.SizeScale = GetBoundBoxMaxWidth(m);
        //            }
        //        }
        //    }

        //}

        //private void view1_MouseUp3D(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext is DynamicReflectionMap3DViewModel vm)
        //    {
        //        var ev = e as MouseUp3DEventArgs;
        //        if (ev != null && !ev.Handled)
        //        {
        //            vm.SelectObject = ev.HitTestResult?.ModelHit;

        //            //if (ev.HitTestResult != null
        //            //    && ev.HitTestResult.ModelHit is MeshGeometryModel3D m)
        //            //{

        //            //    vm.Target = null;
        //            //    vm.CenterOffset = m.Geometry.Bound.Center; // Must update this before updating target
        //            //    vm.Target = ev.HitTestResult.ModelHit as Element3D;
        //            //}
        //        }
        //    }
        //}

        private double GetBoundBoxMaxWidth(MeshGeometryModel3D meshModel)
        {
            var geometry = meshModel.Geometry as MeshGeometry3D;
            var boundingBox = geometry.Bound;
            var width = boundingBox.Size.X;
            var height = boundingBox.Size.Y;
            var depth = boundingBox.Size.Z;

            var maxDimension = Math.Max(width, Math.Max(height, depth));
            return maxDimension;
        }


    }
}
