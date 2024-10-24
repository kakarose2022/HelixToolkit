using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using Rhino.Geometry;
using RobotLib;
using SharpDX;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = SharpDX.Color;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.OrthographicCamera;
using Plane = Rhino.Geometry.Plane;
using Transform = Rhino.Geometry.Transform;

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

        private List<LineGeometry3D> axisModels;

        public List<LineGeometry3D> AxisModels
        {
            get { return axisModels; }
            set 
            { 
                Set(ref axisModels, value);
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

        public delegate void ChangePostionByUiEvent(List<LineGeometry3D> LineGeometry3Ds);
        public event ChangePostionByUiEvent? ChangePostionByUi;

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
            AxisModels = new List<LineGeometry3D>();
            EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");
            //重要
            EffectsManager = new DefaultEffectsManager();
            Camera = new OrthographicCamera()
            {
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, 0),
                Position = new System.Windows.Media.Media3D.Point3D(0, 10, 0),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1),
                FarPlaneDistance = 20000,
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
            //Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
            //{
            //    Position = new Point3D(950, 1000, 1700),
            //    LookDirection = new Vector3D(-950, -1000, -1600), // 看向 XY 平面
            //    UpDirection = new Vector3D(-0.5, -0.5, 0.5),       // Z 轴作为 "上" 的方向
            //    NearPlaneDistance = 0.1,
            //    FarPlaneDistance = 20000,
            //    FieldOfView = 90,
            //};


            AxisModels.Clear();
            Transform T0 = Transform.Translation(new Vector3d(0, 0, 0)) *
            Transform.Rotation(0, new Vector3d(1, 0, 0), new Point3d(0, 0, 0)) *  // 绕 Z 轴旋转 45 度
            Transform.Rotation(0, new Vector3d(0, 1, 0), new Point3d(0, 0, 0)) *  // 绕 Y 轴旋转 30 度
            Transform.Rotation(0, new Vector3d(0, 0, 1), new Point3d(0, 0, 0));    // 绕 X 轴旋转 60 度

            // 定义结束变换矩阵
            Transform T1 = Transform.Translation(new Vector3d(100, 150, 200)) *
                            Transform.Rotation(Math.PI , new Vector3d(1, 0, 0), new Point3d(0, 0, 0)) *  // 绕 Z  轴旋转 90 度
                            Transform.Rotation(Math.PI / 2, new Vector3d(0, 1, 0), new Point3d(0, 0, 0)) *  // 绕 Y 轴旋转 45 度
                            Transform.Rotation(Math.PI / 6, new Vector3d(0, 0, 1), new Point3d(0, 0, 0));    // 绕 X 轴旋转 90 度

            List<Transform> interpolatedTransforms = HelixSharpDemo.Model.Rhino.LinePoseInterp(T0, T1, 100);
            List<Plane> list = new List<Plane>();
            for (int i = 0; i < interpolatedTransforms.Count; i++)
            {
                Transform t = interpolatedTransforms[i];
                Plane tp = t.ToPlane();
                list.Add(tp);

                var x = (float)tp.OriginX;
                var y = (float)tp.OriginY;
                var z = (float)tp.OriginZ;



                Point3d origin = tp.Origin;


                Point3d xAxisEnd = origin + tp.XAxis * 10;


                Point3d yAxisEnd = origin + tp.YAxis * 10;


                Point3d zAxisEnd = origin + tp.ZAxis * 10;


                var length = 10;  // 定义线段的长度
                var lineBuilder = new LineBuilder();
                // 第一条线，沿 X 轴方向
                lineBuilder.AddLine(new Vector3(x, y, z), new Vector3((float)xAxisEnd.X, (float)xAxisEnd.Y, (float)xAxisEnd.Z));
                // 第二条线，沿 Y 轴方向
                lineBuilder.AddLine(new Vector3(x, y, z), new Vector3((float)yAxisEnd.X, (float)yAxisEnd.Y, (float)yAxisEnd.Z));
                // 第三条线，沿 Z 轴方向
                lineBuilder.AddLine(new Vector3(x, y, z), new Vector3((float)zAxisEnd.X, (float)zAxisEnd.Y, (float)zAxisEnd.Z));
                AxisModel = lineBuilder.ToLineGeometry3D(false);
                AxisModel.Colors = new Color4Collection(AxisModel.Positions.Count);
                AxisModel.Colors.Add(Colors.Red.ToColor4());
                AxisModel.Colors.Add(Colors.Red.ToColor4());
                AxisModel.Colors.Add(Colors.Green.ToColor4());
                AxisModel.Colors.Add(Colors.Green.ToColor4());
                AxisModel.Colors.Add(Colors.Blue.ToColor4());
                AxisModel.Colors.Add(Colors.Blue.ToColor4());

                AxisModels.Add(AxisModel);
            }

            ChangePostionByUi?.Invoke(AxisModels);

            //var x = new Random().Next(0, 100);
            //var y = new Random().Next(0, 100);
            //var z = new Random().Next(0, 100);

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
