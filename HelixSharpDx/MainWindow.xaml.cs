using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.Text;
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
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using GeometryModel3D = HelixToolkit.Wpf.SharpDX.GeometryModel3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace HelixSharpDx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            var cube = CreateCube(new Point3D(0, 0, 0), SharpDX.Color.Red);

            //var diffuse = TextureModel.Create("TextureCheckerboard2.jpg");
            //var material = new DiffuseMaterialCore() { DiffuseColor = SharpDX.Color.Red, DiffuseMap = diffuse };
            //cube.Material = material;  // 确保材质应用到模型上


 
            // 设置视图
            var MainCamera = new PerspectiveCamera 
            { 
                Position = new Point3D(8, 9, 7), 
                LookDirection = new Vector3D(-5, -12, -5), 
                UpDirection = new Vector3D(0, 1, 0)
            };
            myViewport.Camera = MainCamera;

            myViewport.CoordinateSystemLabelForeground = new System.Windows.Media.Color() { A = 64, R = 192, G = 192, B = 192 };
            //myViewport.Items.Add(new DirectionalLight3D() { Direction = new Vector3D(-1, -1, -1) });
            //myViewport.Items.Add(new AmbientLight3D() { Color = System.Windows.Media.Color.FromArgb(255, 50, 50, 50) });

            myViewport.Items.Add(cube);

        }

        private GeometryModel3D CreateCube(Point3D center, SharpDX.Color color)
        {
            var sphere = new MeshBuilder();
            sphere.AddSphere(new Vector3(0, 0, 0), 0.2);
           var aa  = sphere.ToMeshGeometry3D();
            //var material = new DiffuseMaterial(new SolidColorBrush(color)); // 设置颜色材质


            var b1 = new MeshBuilder(true, true, true);
            b1.AddSphere(new Vector3(0.25f, 0.25f, 0.25f), 0.75, 64, 64);
            b1.AddBox(-new Vector3(0.25f, 0.25f, 0.25f), 1, 1, 1, BoxFaces.All);
            b1.AddBox(-new Vector3(5.0f, 0.0f, 0.0f), 1, 1, 1, BoxFaces.All);
            b1.AddSphere(new Vector3(5f, 0f, 0f), 0.75, 64, 64);
            b1.AddCylinder(new Vector3(0f, -3f, -5f), new Vector3(0f, 3f, -5f), 1.2, 64);


            return new MeshGeometryModel3D()
            {
                Geometry = sphere.ToMesh(),
            };
        }

        // 创建球体的方法
        //private GeometryModel3D CreateSphere(Point3D center, Color color)
        //{
        //    var meshBuilder = new MeshBuilder();
        //    meshBuilder.AddSphere(center, 0.5, 20, 20);
        //    var mesh = meshBuilder.ToMesh();

        //    var material = new DiffuseMaterial(new SolidColorBrush(color));
        //    return new GeometryModel3D(mesh, material);
        //}
    }
}