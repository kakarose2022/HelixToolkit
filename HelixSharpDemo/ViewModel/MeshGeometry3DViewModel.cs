using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.Windows.Input;
using System.Windows.Media;
using Color = SharpDX.Color;

namespace HelixSharpDemo.ViewModel
{
    public class MeshGeometry3DViewModel : BaseViewModel
    {
        private string selectType;

        public string SelectType
        {
            get { return selectType; }
            set
            {
                if (Set(ref selectType, value))
                {
                    AddMeshModel();
                }
            }
        }

        public TextureModel EnvironmentMap { private set; get; }

        #region Helix 3D
        private LineGeometry3D axisModel;
        public LineGeometry3D AxisModel
        {
            get { return axisModel; }
            set
            {
                Set(ref axisModel, value);
            }
        }

        private MeshGeometry3D meshModel;
        public MeshGeometry3D MeshModel
        {
            get { return meshModel; }
            set
            {
                Set(ref meshModel, value);
            }
        }

        private Geometry3D axisLines;

        public Geometry3D AxisLines
        {
            get { return axisLines; }
            set 
            {
                Set(ref axisLines, value);
            }
        }

        private BillboardImage3D axisLabels;
            
        public BillboardImage3D AxisLabels
        {
            get { return axisLabels; }
            set 
            {
                Set(ref axisLabels, value);
            }
        }

        #endregion


        public ICommand AddCoordiateCommand { private set; get; }
        public ICommand AddSigleCoordiateCommand { private set; get; }

        public MeshGeometry3DViewModel()
        {
            Init();
        }

        public void Init()
        {
            AxisModel = new LineGeometry3D();
            MeshModel = new MeshGeometry3D();
            EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");
            //重要
            EffectsManager = new DefaultEffectsManager();
            Camera = new OrthographicCamera()
            {
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, 0),
                Position = new System.Windows.Media.Media3D.Point3D(0, 10, 0),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1),
                FarPlaneDistance = 1000,
                NearPlaneDistance = 0.1f
            };

            AddCoordiateCommand = new RelayCommand(o =>
            {
                AddCoodiate();
            });
            AddSigleCoordiateCommand = new RelayCommand(o =>
            {
                AddCoodiateSystem();
            });

        }

        public void AddCoodiateSystem()
        {
            var linebuilder = new LineBuilder();
            linebuilder.AddLine(Vector3.Zero, Vector3.UnitX * 6);
            linebuilder.AddLine(Vector3.Zero, Vector3.UnitY * 6);
            linebuilder.AddLine(Vector3.Zero, Vector3.UnitZ * 6);
            AxisLines = linebuilder.ToLineGeometry3D();
            AxisLines.Colors = new Color4Collection() { Color.Red, Color.Red, Color.Green, Color.Green, Color.Blue, Color.Blue };
            var texts = new TextInfoExt[]
            {
                new TextInfoExt(){Text = "左", Origin = Vector3.UnitX * 8, Foreground = Color.Red, Size = 16, FontWeight = SharpDX.DirectWrite.FontWeight.SemiBold },
                new TextInfoExt(){Text = "前", Origin= Vector3.UnitY * 8 , Foreground = Color.Green, Size = 16, FontWeight = SharpDX.DirectWrite.FontWeight.SemiBold},
                new TextInfoExt(){Text = "上", Origin = Vector3.UnitZ * 8, Foreground = Color.Blue, Size = 16, FontWeight = SharpDX.DirectWrite.FontWeight.SemiBold }
            };
            AxisLabels = texts.ToBillboardImage3D(EffectsManager);
        }

        public void AddCoodiate()
        {
            //var x = new Random().Next(0, 100);
            //var y = new Random().Next(0, 100);
            //var z = new Random().Next(0, 100);
            var x = 0;
            var y = 0;
            var z = 0;

            var length = 10;  // 定义线段的长度
            var lineBuilder = new LineBuilder();
            // 第一条线，沿 X 轴方向
            lineBuilder.AddLine(new Vector3(x, y, z), new Vector3(x + length, y, z));
            // 第二条线，沿 Y 轴方向
            lineBuilder.AddLine(new Vector3(x, y, z), new Vector3(x, y + length, z));
            // 第三条线，沿 Z 轴方向
            lineBuilder.AddLine(new Vector3(x, y, z), new Vector3(x, y, z + length));
            AxisModel = lineBuilder.ToLineGeometry3D(false);
            AxisModel.Colors = new Color4Collection(AxisModel.Positions.Count);
            AxisModel.Colors.Add(Colors.Red.ToColor4());
            AxisModel.Colors.Add(Colors.Red.ToColor4());
            AxisModel.Colors.Add(Colors.Green.ToColor4());
            AxisModel.Colors.Add(Colors.Green.ToColor4());
            AxisModel.Colors.Add(Colors.Blue.ToColor4());
            AxisModel.Colors.Add(Colors.Blue.ToColor4());
        }

        public void AddMeshModel()
        {
            var meshBuilder = new MeshBuilder();
            var localOrigin = new Vector3(0, 0, 0);
            meshBuilder.AddBox(localOrigin, 5, 5, 5);
            meshBuilder.ToMeshGeometry3D();
            MeshModel = meshBuilder.ToMeshGeometry3D();

            MeshModel.Colors = new Color4Collection(MeshModel.Positions.Count);
            MeshModel.Colors.Add(Colors.Red.ToColor4());
            MeshModel.Colors.Add(Colors.Red.ToColor4());
            MeshModel.Colors.Add(Colors.Green.ToColor4());
            MeshModel.Colors.Add(Colors.Green.ToColor4());
            MeshModel.Colors.Add(Colors.Blue.ToColor4());
            MeshModel.Colors.Add(Colors.Blue.ToColor4());
            MeshModel.UpdateOctree();
        }
    }
}
