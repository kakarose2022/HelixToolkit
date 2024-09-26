using HelixToolkit.SharpDX.Core;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Media3D = System.Windows.Media.Media3D;
using Colors = System.Windows.Media.Colors;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using HelixToolkit.Wpf.SharpDX;

namespace HelixSharpDemo
{
    public class MainWindowModel:BaseViewModel
    {
        public PointGeometry3D PointsModel { private set; get; }
        public MeshGeometry3D DefaultModel { private set; get; }
        public LineGeometry3D AxisModel { get; private set; }
        public System.Windows.Media.Color PointColor{ get { return Colors.Green; } }
        public System.Windows.Media.Color AmbientLightColor { get; set; }
        public System.Windows.Media.Color Light1Color { get; set; }

        public ICommand RefreshCommand { private set; get; }
        public ICommand LineBuliderCommand { private set; get; }
        public TextureModel EnvironmentMap { private set; get; }

        private BillboardText3D meshTitles;

        public BillboardText3D MeshTitles
        {
            get { return meshTitles; }
            set 
            {
                Set(ref meshTitles, value);
            }
        }


        private Vector3D light1Direction = new Vector3D();
        public Vector3D Light1Direction
        {
            set
            {
                if (light1Direction != value)
                {
                    Set(ref light1Direction, value);
                }
            }
            get
            {
                return light1Direction;
            }
        }

        public MainWindowModel()
        {
            RefreshCommand = new RelayCommand(o =>
            {
                Refresh();
            });
            LineBuliderCommand = new RelayCommand(o =>
            {
                LineBuilder();
            });

            this.Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
            {
                Position = new Point3D(30, 30, 30),
                LookDirection = new Vector3D(-30, -30, -30),
                UpDirection = new Vector3D(0, 1, 0)
            };
            EffectsManager = new DefaultEffectsManager();
            Refresh();
        }

        public void LineBuilder()
        {
            var lineBuilder = new LineBuilder();
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(10, 0, 0));
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 10, 0));
            lineBuilder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 0, 10));
            AxisModel = lineBuilder.ToLineGeometry3D();
            AxisModel.Colors = new Color4Collection(AxisModel.Positions.Count);
            AxisModel.Colors.Add(Colors.Red.ToColor4());
            AxisModel.Colors.Add(Colors.Red.ToColor4());
            AxisModel.Colors.Add(Colors.Green.ToColor4());
            AxisModel.Colors.Add(Colors.Green.ToColor4());
            AxisModel.Colors.Add(Colors.Blue.ToColor4());
            AxisModel.Colors.Add(Colors.Blue.ToColor4());




        }


        public void Refresh()
        {
            this.Title = "DynamicTexture Demo";
            this.SubTitle = "WPF & SharpDX";
            this.AmbientLightColor = Colors.DimGray;
            this.Light1Direction = new Vector3D(-10, -10, -10);
            this.Light1Color = Colors.White;
            this.EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");

            LineBuilder();

            var b2 = new MeshBuilder(true, true, true);
            //b2.AddSphere(new Vector3(15f, 0f, 0f), 4, 64, 64);
            b2.AddSphere(new Vector3(25f, 0f, 0f), 2, 32, 32);
            b2.AddBox(new Vector3(25f, 20f, 20f),10,15,20 );

            //b2.AddTube(new Vector3[] { new Vector3(10f, 5f, 0f), new Vector3(10f, 7f, 0f) }, 2, 12, false, true, true);
            DefaultModel = b2.ToMeshGeometry3D();
            //DefaultModel.OctreeParameter.RecordHitPathBoundingBoxes = true;

            PointsModel = new PointGeometry3D();
            var offset = new Vector3(1, 1, 1);

            PointsModel.Positions = new Vector3Collection(DefaultModel.Positions.Select(x => x + offset));
            PointsModel.Indices = new IntCollection(Enumerable.Range(0, PointsModel.Positions.Count));
            //PointsModel.OctreeParameter.RecordHitPathBoundingBoxes = true;

            Transform3D Transform1 = new Media3D.TranslateTransform3D(0, 0, 0);
            MeshTitles = new BillboardText3D();

            MeshTitles.TextInfo.Add(new TextInfo("12", ToVector3(Transform1)));
        }

        public  Vector3 ToVector3( Transform3D trafo)
        {
            var matrix = trafo.Value;
            return new Vector3((float)matrix.OffsetX, (float)matrix.OffsetY, (float)matrix.OffsetZ);
        }
    }


}
