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
using System.Windows.Input.Manipulations;

namespace HelixSharpDemo.View
{
    /// <summary>
    /// DynamicReflectionMap3DView.xaml 的交互逻辑
    /// </summary>
    public partial class DynamicReflectionMap3DView : UserControl
    {
        public DynamicReflectionMap3DViewModel vm { get; set; }

        public DynamicReflectionMap3DView()
        {
            InitializeComponent();
            this.DataContext = new DynamicReflectionMap3DViewModel(view1);
            vm = (DynamicReflectionMap3DViewModel?)this.DataContext;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                vm.ChangeDyContent -= AttatchToDynamic;
                vm.ChangeDyContent += AttatchToDynamic;
                vm.OnPlay -= Test;
                vm.OnPlay += Test;
                vm.ChangePostionByUi -= Test;
                vm.ChangePostionByUi += Test;
                vm.ChangeSliderBarValue -= ChangeSliderBar;
                vm.ChangeSliderBarValue += ChangeSliderBar;
            }
        }

        public void AttatchToDynamic()
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                //viewport
                Dynamicc.Children.Clear();
                //菜单栏
                newS.Children.Clear();

                var paramsss= new List<double>();
                paramsss.Clear();
                vm.MeshGeometryModel3Ds.Clear();

                foreach (var sceneNode in vm.SceneNodes)
                {
                    var oneSceneMeshGeometry3Ds = vm.SceneNodeToMeshGeometry3D(sceneNode.Root);
                    foreach (var meshGeometryModel3D in oneSceneMeshGeometry3Ds)
                    {
                        Dynamicc.Children.Add(meshGeometryModel3D);
                        paramsss.Add(AddSliderBar());
                    }
                }

                Button button = new Button();
                button.Command = vm.RobotPlayCommand;
                button.CommandParameter = paramsss;
                button.Content = "PLAY";
                newS.Children.Add(button);
            }
        }

        public void Test(List<Matrix3D> matrix3Ds, bool needTransform)
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                int index = 0;

                var aa = transformManipulator3D.Transform; 

                foreach (var child in Dynamicc.Children)
                {
                    if(child is Element3D element3d)
                    {                     
                       if (element3d.Transform is MatrixTransform3D group)
                       {
                            //group.Matrix = matrix3Ds[index];
                            //if (index != 1)
                            //{
                            //    //group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;
                            //    group.Matrix = matrix3Ds[index];
                            //}

                            //group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;

                            //if (index != 1)
                            //{
                            //    group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;
                            //}
                            //else
                            //{
                            //    //group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;
                            //}

                            group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;
                            //group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;

                            //group.Matrix = matrix3Ds[index] * vm.DefaultMatrix3D;

                            index++;

                            //group.Matrix = group.Matrix * new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 270)).Value;
                            //group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(1, 0, 0), 270)));
                       }
                    }
                    else
                    {
                    }
                }
            }
        }

        private void ChangeSliderBar(float sliderBarValue)
        {
           var sliderbars =  FindSlidersInStackPanel(newS);
            if (sliderbars !=null)
            {
                sliderbars[0].Value = sliderBarValue;
            }
        }

        private double AddSliderBar()
        {
            Slider newSlider = new Slider
            {
                Minimum = -150,
                Maximum = 150,
                Value = 0,
                Width = 110,
                Height = 26,
                TickFrequency = 1,
                
            };

            TextBox textBox = new TextBox();
            textBox.Text = newSlider.Value.ToString("F2");
            newSlider.ValueChanged += (s, e) =>
            {
                textBox.Text = newSlider.Value.ToString("F2");
                LoadPostion();
            };
            textBox.TextChanged += (s, e) =>
            {
                if (double.TryParse(textBox.Text, out double value))
                {
                    newSlider.Value = value;
                }
            };
            newS.Children.Add(newSlider);
            newS.Children.Add(textBox);
            return newSlider.Value;
        }

        private void LoadPostion()
        {
            if (this.DataContext is DynamicReflectionMap3DViewModel vm)
            {
                //var sliders = newS.Children.to.Select(o => o is Slider slider);
                List<Slider> sliders = new List<Slider>();
                foreach (var child in newS.Children)
                {
                    if(child is Slider s)
                    {
                        sliders.Add(s);
                    }
                }

                List<double> values = new List<double>();
                foreach (var slider in sliders)
                {
                    values.Add(slider.Value);
                }

                var matrix3Ds =  vm.GetMatrix3s(values.ToArray());
                Test(matrix3Ds, false);
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


        public static List<Slider> FindSlidersInStackPanel(StackPanel stackPanel)
        {
            List<Slider> sliders = new List<Slider>();
            FindSlidersRecursive(stackPanel, sliders);
            return sliders;
        }

        private static void FindSlidersRecursive(DependencyObject parent, List<Slider> sliders)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Slider slider)
                {
                    sliders.Add(slider);
                }

                // 继续递归查找子元素
                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    FindSlidersRecursive(child, sliders);
                }
            }
        }
    }
}
