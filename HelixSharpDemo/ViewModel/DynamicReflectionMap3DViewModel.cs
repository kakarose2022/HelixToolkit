using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Color = SharpDX.Color;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using GeometryModel3D = HelixToolkit.Wpf.SharpDX.GeometryModel3D;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using Matrix = SharpDX.Matrix;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Quaternion = SharpDX.Quaternion;
using Vector3 = SharpDX.Vector3;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace HelixSharpDemo.ViewModel
{
    public class DynamicReflectionMap3DViewModel : BaseViewModel
    {
        private object selectObject;
        public object SelectObject
        {
            get { return selectObject; }
            set 
            {
                if (selectObject != value)
                {
                    PostSelectedEffect(selectObject, false);
                    if (Set(ref selectObject, value))
                    {
                        PostSelectedEffect(selectObject, selectObject != null);

                        if(selectObject is Element3D element3D)
                        {
                            element3D.SceneNode.TransformChanged -= SceneNode_OnTransformChanged;
                            element3D.SceneNode.TransformChanged += SceneNode_OnTransformChanged;
                        }
                    }
                }
            }
        }

        private void SceneNode_OnTransformChanged(object? sender, TransformArgs e)
        {
            var m = e.Transform;
            m.Decompose(out var scale, out var rotation, out var translation);
            var scaleMatrix = Matrix.Scaling(scale);
            var rotationMatrix = Matrix.RotationQuaternion(rotation);

            var aa = AngleToDegree(rotation);
            if (aa > 150 || aa < -150)
            {
                TextInfo = $"机器人越界 角度 {aa}  弧度{rotation.Angle}。。。";
            }
            else
            {
                TextInfo = $"机器人正常角度 角度 {aa}  弧度{rotation.Angle}。。。";
            }

            //Trace.WriteLine($" scaleMatrix {scaleMatrix}");
            //Trace.WriteLine($" rotationMatrix {rotationMatrix}");

            //if (centerOffset != Vector3.Zero)
            //{
            //    var org = Matrix.Translation(-centerOffset) * scaleMatrix * rotationMatrix * Matrix.Translation(centerOffset);
            //    translationVector = translation - org.TranslationVector;
            //}
            //else
            //{
            //    translationVector = m.TranslationVector;
            //}
            //OnUpdateSelfTransform();
        }

        private Geometry3D selectedGeometry;
        public Geometry3D SelectedGeometry
        {
            get { return selectedGeometry; }
            set
            {
                Set(ref selectedGeometry, value);
            }
        }

        private bool isLoading = false;
        public bool IsLoading
        {
            private set => Set(ref isLoading, value);
            get => isLoading;
        }

        private string textInfo;
        public string TextInfo
        {
            get { return textInfo; }
            set
            {
                Set(ref textInfo, value);
            }
        }

        private int selectIndex = 0;
        public int SelectIndex
        {
            get { return selectIndex; }
            set
            {
                if(Set(ref selectIndex, value))
                {
                    ReloadCameraCommand?.Execute(null);
                }
            }
        }

        private PBRMaterial pBRMaterial;
        public PBRMaterial PBRMaterial
        {
            get { return pBRMaterial; }
            set
            {
                Set(ref pBRMaterial, value);
            }
        }

        /// <summary>
        ///  所有导入物件的Scene
        /// </summary>
        private List<HelixToolkitScene> dyGeometry3d;
        public List<HelixToolkitScene> SceneNodes
        {
            get { return dyGeometry3d; }
            set
            {
                Set(ref dyGeometry3d, value);
            }
        }

        private SceneNodeGroupModel3D groupModel;
        public SceneNodeGroupModel3D GroupModel
        {
            get { return groupModel; }
            set 
            {
                Set(ref groupModel, value); 
            }
        }

        public Dictionary<string, MeshGeometryModel3D> MeshGeometryModel3Ds { get; set; }
        private HelixToolkitScene scene;

        private bool renderEnvironmentMap = true;
        public bool RenderEnvironmentMap
        {
            set
            {
                if (Set(ref renderEnvironmentMap, value) 
                    && scene != null 
                    && scene.Root != null)
                {
                    foreach (var node in scene.Root.Traverse())
                    {
                        if (node is MaterialGeometryNode m && m.Material is PBRMaterialCore material)
                        {
                            material.RenderEnvironmentMap = value;
                        }
                    }
                }
            }
            get => renderEnvironmentMap;
        }

        #region HitResult
        private Element3D target;
        public Element3D Target
        {
            get { return target; }
            set 
            { 
                Set(ref target, value); 
            }
        }

        private Vector3 centerOffset;
        public Vector3 CenterOffset
        {
            get { return centerOffset; }
            set
            {
                Set(ref centerOffset, value);
            }
        }

        private double sizeScale;
        public double SizeScale
        {
            get { return sizeScale; }
            set 
            {
                Set(ref sizeScale, value);
            }
        }

        private Transform3D manipulatorTransform;
        public Transform3D ManipulatorTransform
        {
            get { return manipulatorTransform; }
            set 
            {
                Set(ref manipulatorTransform, value);
            }
        }

        private Matrix PreMatrix;
        private Matrix CurrentMatrix;

        #endregion

        #region 运动
        private RotateTransform3D R = new RotateTransform3D();
        private TranslateTransform3D T = new TranslateTransform3D();
        #endregion

        public ICommand SetCameraPostionCommand { private set; get; }
        public ICommand ReloadCameraCommand { private set; get; }
        public ICommand RobotPlayCommand { private set; get; }
        public ICommand ResetManipulatorCommand { private set; get; }

        public delegate void ChangeDyContentEvent();
        public event ChangeDyContentEvent? ChangeDyContent;

        public delegate void OnPlayEvent(List<Matrix3D> matrix3Ds);
        public event OnPlayEvent? OnPlay;

        public delegate void ChangePostionByUiEvent(List<Matrix3D> matrix3Ds);
        public event ChangePostionByUiEvent? ChangePostionByUi;


        private HelixToolkit.Wpf.SharpDX.Viewport3DX viewport { set; get; }

        #region transform
        public Point3D CurrentPostion {  set; get; }
        public Matrix3D DefaultMatrix3D { set; get; }
        public Transform3DGroup F1 { get; set; }
        public Transform3DGroup F2 { get; set; }
        public Transform3DGroup F3 { get; set; }
        public Transform3DGroup F4 { get; set; }
        public Transform3DGroup F5 { get; set; }
        public Transform3DGroup F6 { get; set; }
        public Transform3DGroup F7 { get; set; }
        public Transform3DGroup F8 { get; set; }
        public Transform3DGroup F9 { get; set; }
        public Transform3DGroup F10 { get; set; }
        #endregion

        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        public DynamicReflectionMap3DViewModel(HelixToolkit.Wpf.SharpDX.Viewport3DX viewport)
        {
            SceneNodes = new List<HelixToolkitScene>();
            GroupModel = new SceneNodeGroupModel3D();
            MeshGeometryModel3Ds = new Dictionary<string, MeshGeometryModel3D>();
           
            // 沿着x轴旋转
            DefaultMatrix3D = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 270)).Value 
                               * new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0)).Value;

            var combinedTransform = new Transform3DGroup();
            combinedTransform.Children.Add(new MatrixTransform3D(DefaultMatrix3D));
            ManipulatorTransform = combinedTransform;

            PreMatrix = new Matrix();
            CurrentMatrix = new Matrix();

            CurrentPostion = new Point3D();
            InitRobotTransform();
            InitSetting();
            ReloadFile();
            this.viewport = viewport;
        }

        protected  override void InitSetting()
        {
            this.Camera = new PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(300, 420, -20),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(-10, -200, -200),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                NearPlaneDistance = 0.1,
                FieldOfView = 45,
                FarPlaneDistance = 10000
            };

            SetCameraPostionCommand = new RelayCommand(o =>
            {
                SetDefaultCameraView();
            });

            ReloadCameraCommand= new RelayCommand(o =>
            {
                ReloadFile();
            });

            RobotPlayCommand = new RelayCommand(async o =>
            {
                //await OnMove(o);
                Move(GetMatrix3s(new double[6] {0,0,0,0,0,0}));
            });

            ResetManipulatorCommand = new RelayCommand( o =>
            {
                ResetManipulatorDirection();
            });

            base.InitSetting();
            Reload();
        }

        protected async Task ReloadFile()
        {
            SceneNodes.Clear();
            GroupModel.Clear();
            var path = Path.Combine(Environment.CurrentDirectory, "stl");
            if(selectIndex == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                     await LoadModelFile(Path.Combine(path, $"p{i}.stl"));
                }
            }
            else if (selectIndex == 1)
            {
                await LoadModelFile(Path.Combine(path, "Goku No Support by MetaLWarrioR.stl"));
            }
            else if (selectIndex == 2)
            {
                //await LoadModelFile(Path.Combine(path, $"p0.stl"));
                for (int i = 0; i < 10; i++)
                {
                    await LoadModelFile(Path.Combine(path, $"part{i}.stl"));
                }
            }
            else if (selectIndex == 3)
            {
                for (int i = 0; i < 10; i++)
                {
                    await LoadModelFile(Path.Combine(path, $"part{i}.stl"));
                }
            }

            Reload();
        }

        public void InitDefaultCoordiate()
        {
            var builder = new MeshBuilder(true, false, false);
            builder.AddArrow(Vector3.Zero, new Vector3(5.5f, 0, 0), 0.6f, 1.7f, 8);
            builder.AddArrow(Vector3.Zero, new Vector3(0, 5.5f, 0), 0.6f, 1.7f, 8);
            builder.AddArrow(Vector3.Zero, new Vector3(0, 0, 5.5f), 0.6f, 1.7f, 8);

            var mesh = builder.ToMesh();
            SelectedGeometry = mesh;
            //UpdateAxisColor(CoorNode.Geometry, 0, AxisXColor, LabelX, LabelColor);
            //UpdateAxisColor(arrowMCoorNodeeshModel.Geometry, 1, AxisYColor, LabelY, LabelColor);
            //UpdateAxisColor(CoorNode.Geometry, 2, AxisZColor, LabelZ, LabelColor);

            //AxisLines = meshBuilder.ToMeshGeometry3D();
            //AxisLines.Colors = new Color4Collection() { Color.Red, Color.Red, Color.Green, Color.Green, Color.Blue, Color.Blue };

            //var texts = new TextInfoExt[]
            //{
            //    new TextInfoExt(){Text = "左", Origin = Vector3.UnitX * 8, Foreground = Color.Red, Size = 16, FontWeight = SharpDX.DirectWrite.FontWeight.SemiBold },
            //    new TextInfoExt(){Text = "前", Origin= Vector3.UnitY * 8 , Foreground = Color.Green, Size = 16, FontWeight = SharpDX.DirectWrite.FontWeight.SemiBold},
            //    new TextInfoExt(){Text = "上", Origin = Vector3.UnitZ * 8, Foreground = Color.Blue, Size = 16, FontWeight = SharpDX.DirectWrite.FontWeight.SemiBold }
            //};
            //AxisLabels = texts.ToBillboardImage3D(EffectsManager);
        }

        private void SetDefaultCameraView()
        {
            //Camera = new OrthographicCamera()
            //{
            //    LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 10, 10),
            //    Position = new System.Windows.Media.Media3D.Point3D(0, 6000, 6000),
            //    UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            //    FarPlaneDistance = 10000,
            //    NearPlaneDistance = -1000
            //};

            this.Camera = new PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(446, 3000, 2500),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(-138, -2777, -2777),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                NearPlaneDistance = 0.1,
                FieldOfView = 45,
                FarPlaneDistance = 20000
            };

            //this.Camera = new OrthographicCamera
            //{
            //    Position = new System.Windows.Media.Media3D.Point3D(446, 3000, 2500),
            //    LookDirection = new System.Windows.Media.Media3D.Vector3D(-138, -2777, -2777),
            //    UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            //    NearPlaneDistance = -0.1f,
            //    FarPlaneDistance = 10000,
            //};
        }

        public void Reload()
        {
            ChangeDyContent?.Invoke();
        }

        public async Task<HelixToolkitScene> LoadModelFile(string path)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                IsLoading = true;
                TextInfo = "加载中....";
                try
                {
                    var loader = new Importer();
                    var loadedScene = await Task.Run(() => loader.Load(path));

                    if (loadedScene != null)
                    {
                        if (loadedScene.Root != null)
                        {
                            foreach (var node in loadedScene.Root.Traverse())
                            {
                                if (node is MaterialGeometryNode m)
                                {
                                    if (m.Material is PBRMaterialCore pbr)
                                    {
                                        pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                    }
                                    else if (m.Material is PhongMaterialCore phong)
                                    {
                                        phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                    }
                                    m.Material  = ConvertMaterial(m.Material);
                                }
                            }
                        }

                        RobotInitTransform(path, loadedScene?.Root);
                        loadedScene.Root.Tag = loadedScene?.Root.ModelMatrix;

                        //SceneNodeToMeshGeometry3D(loadedScene?.Root);

                        //if (loadedScene.HasAnimation)
                        //{
                        //    foreach (var ani in loadedScene.Animations)
                        //    {
                        //        Animations.Add(ani);
                        //    }
                        //}
                    }
                    TextInfo = "加载完成";
                    SceneNodes.Add(loadedScene);

                   var animations = loadedScene?.Animations.Select(x => x.Name).ToArray();

                    return loadedScene;
                }
                catch (Exception ex)
                {
                    TextInfo = "加载失败";
                    MessageBox.Show(ex.Message);
                    return null;
                }
                finally
                {
                    IsLoading = false;
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        #region 获取所有节点
        public IEnumerable<Element3D> SceneNodeToMeshGeometry3D(SceneNode node)
        {
            foreach (var single in node.Traverse())
            {
                if(single is MeshNode meshNode)
                {
                    //continue;
                    var model = new MeshGeometryModel3D
                    {
                        Geometry = meshNode.Geometry,
                        CullMode = SharpDX.Direct3D11.CullMode.Back,
                        Material = ConvertMaterial1(meshNode.Material),
                        IsThrowingShadow = false,
                        ////将模型的变换矩阵转换为 WPF 所需的 Matrix3D 格式
                        // node.ModelMatrix
                        Transform = new MatrixTransform3D(node.ModelMatrix.ToMatrix3D())
                    };

                    MeshGeometryModel3Ds.Add(model.GUID.ToString(), model);
                    yield return model;
                }
                else
                {
                    //GroupModel.AddNode(node);
                }
            }
        }
   
        /// <summary>
        /// 模型翻转立正
        /// </summary>
        /// <param name="model"></param>
        private void Transform(MeshGeometryModel3D model, Matrix3D matrix3D)
        {
            CurrentPostion = model.Transform.Transform(new Point3D(0, 0, 0));
            var rotationX = new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(1, 0, 0), 270));
            var rotationY = new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(0, 1, 0), 0));

            var combinedTransform = new Transform3DGroup();
            combinedTransform.Children.Add(rotationX);
            combinedTransform.Children.Add(rotationY);
            combinedTransform.Children.Add(new MatrixTransform3D(matrix3D));
            model.Transform = combinedTransform;
        }

        private bool IsUIMeshGeometryModel3D(MeshGeometryModel3D model)
        {
           //var aa = SceneNodes.SelectMany(o => SceneNodeToMeshGeometry3D(o?.Root)).Select(o => o.GUID);
           // return SceneNodes.SelectMany(o => SceneNodeToMeshGeometry3D(o?.Root)).Any(x => x.GUID == model.GUID);
            return MeshGeometryModel3Ds.ContainsKey(model.GUID.ToString());
        }
        #endregion

        private Material ConvertMaterial(MaterialCore materialCore)
        {
            if (materialCore is PBRMaterialCore pbrMaterial)
            {
                return new PBRMaterial(pbrMaterial);
            }
            else if (materialCore is PhongMaterialCore phongMaterial)
            {
                return new PhongMaterial(phongMaterial)
                {
                    // 设置青铜色
                    DiffuseColor = new Color(234,172,158).ToColor4(), // 青铜色
                    //SpecularColor = new Color4(255 / 255f, 255 / 255f, 224 / 255f, 1.0f), // 浅金色高光
                    SpecularShininess = 164, // 提高高光强度
                                        // 可选：增加自发光
                    EmissiveColor = new Color4(0, 0, 0, 0) // 无自发光，可根据需要调整
                };
            }
            else if (materialCore is DiffuseMaterialCore DiffuseMaterial)
            {
                return new DiffuseMaterial(DiffuseMaterial);
            }
            return null; 
        }

        private Material ConvertMaterial1(MaterialCore materialCore)
        {
            if (materialCore is PBRMaterialCore pbrMaterial)
            {
                return new PBRMaterial(pbrMaterial);
            }
            else if (materialCore is PhongMaterialCore phongMaterial)
            {
                return new PhongMaterial(phongMaterial)
                {
                    // 设置青铜色
                    DiffuseColor = Colors.Red.ToColor4(), // 青铜色
                    //SpecularColor = new Color4(255 / 255f, 255 / 255f, 224 / 255f, 1.0f), // 浅金色高光
                    SpecularShininess = 264, // 提高高光强度
                                             // 可选：增加自发光
                    EmissiveColor = new Color4(0, 0, 0, 0) // 无自发光，可根据需要调整
                };
            }
            else if (materialCore is DiffuseMaterialCore DiffuseMaterial)
            {
                return new DiffuseMaterial(DiffuseMaterial);
            }
            return null;
        }

        #region Effect
        private void PostSelectedEffect(object obj, bool selected)
        {
            if (obj == null)
                return;

            //ClearNodesSelectEffect();
            if (obj is GeometryNode)
            {
                var mn = obj as GeometryNode;
                mn.PostEffects = selected ? $"highlight[color:#FFFF00]" : null;
            }
            else if (obj is GeometryModel3D)
            {
                var mn = obj as GeometryModel3D;
                mn.PostEffects = selected ? $"highlight[color:#FFFF00]" : null;
            }
        }

        private void ClearNodesSelectEffect()
        {
            foreach (var node in SceneNodes)
            {
                foreach (var item in node.Root.Traverse())
                {
                    if (item is MeshNode meshNode)
                    {
                        meshNode.ClearPostEffect();
                    }
                }     
            }
        }
        #endregion

        #region 运动
        private async Task OnMove(object robotAngles)
        {
            if(robotAngles is List<double> angles)
            {
                #region Transform
                //Transform3DGroup F1 = new Transform3DGroup();
                //Transform3DGroup F2 = new Transform3DGroup();
                //Transform3DGroup F3 = new Transform3DGroup();
                //Transform3DGroup F4 = new Transform3DGroup();
                //Transform3DGroup F5 = new Transform3DGroup();
                //Transform3DGroup F6 = new Transform3DGroup();
                //Transform3DGroup F7 = new Transform3DGroup();
                //Transform3DGroup F8 = new Transform3DGroup();
                //Transform3DGroup F9 = new Transform3DGroup();
                //Transform3DGroup F10 = new Transform3DGroup();
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), 0.0), new Point3D(0.0, 0.0, 0.0));
                F1.Children.Add(this.R);

                this.T = new TranslateTransform3D(0.0, 0.0, 77.9);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), angles[0]), new Point3D(0.0, 0.0, 77.9));
                F2.Children.Add(this.T);
                F2.Children.Add(this.R);
                F2.Children.Add(F1);

                this.T = new TranslateTransform3D(0.0, 0.0, 66.0);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[1]), new Point3D(0.0, 0.0, 66.0));
                F3.Children.Add(this.T);
                F3.Children.Add(this.R);
                F3.Children.Add(F2);

                this.T = new TranslateTransform3D(0.0, 0.0, 66.0);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), -15.47 + angles[2]), new Point3D(0.0, 0.0, 66.0));
                F4.Children.Add(this.T);
                F4.Children.Add(this.R);
                F4.Children.Add(F2);

                this.T = new TranslateTransform3D(-43.37, 0.0, 54.0);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[1] - angles[2]), new Point3D(-43.37, 0.0, 54.0));
                F5.Children.Add(this.T);
                F5.Children.Add(this.R);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[2]), new Point3D(0.0, 0.0, 64.0));
                F5.Children.Add(this.R);
                F5.Children.Add(F2);

                this.T = new TranslateTransform3D(-81.722, 0.0, 57.63299999999998);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), -angles[1] + angles[2]), new Point3D(-81.722, 0.0, 57.63299999999998));
                F6.Children.Add(this.T);
                F6.Children.Add(this.R);
                F6.Children.Add(F3);

                this.T = new TranslateTransform3D(15.34, 0.0, 31.5);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1.0, 0.0, 0.0), angles[3]), new Point3D(15.34, 0.0, 31.5));
                F7.Children.Add(this.T);
                F7.Children.Add(this.R);
                F7.Children.Add(F6);

                this.T = new TranslateTransform3D(41.5, 3.5, 0.0);
                this.R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[4]), new Point3D(41.5, 3.5, 0.0));
                F8.Children.Add(this.T);
                F8.Children.Add(this.R);
                F8.Children.Add(F7);

                this.T = new TranslateTransform3D(86.925, angles[5] * 0.8, 0.0);
                F9.Children.Add(this.T);
                F9.Children.Add(F8);

                this.T = new TranslateTransform3D(86.925, -angles[5] * 0.8, 0.0);
                F10.Children.Add(this.T);
                F10.Children.Add(F8);
                #endregion

                var transform3DGroup = new List<Transform3DGroup>();
                transform3DGroup.Add(F1);
                transform3DGroup.Add(F2);
                transform3DGroup.Add(F3);
                transform3DGroup.Add(F4);
                transform3DGroup.Add(F5);
                transform3DGroup.Add(F6);
                transform3DGroup.Add(F7);
                transform3DGroup.Add(F8);
                transform3DGroup.Add(F9);
                transform3DGroup.Add(F10);
                await ReloadFile();
            }
        }

        public List<Matrix3D> GetMatrix3s(double[] angles)
        {
            List<Matrix3D> matrices = new List<Matrix3D>();
            matrices.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), 0.0),
                new Point3D(0.0, 0.0, 0.0)).Value);

            matrices.Add(new TranslateTransform3D(0.0, 0.0, 77.9).Value
                          * new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), angles[0]),new Point3D(0.0, 0.0, 77.9)).Value
                          * matrices[0]);

            matrices.Add(new TranslateTransform3D(0.0, 0.0, 66.0).Value
                          * new RotateTransform3D(
                              new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[1]),
                              new Point3D(0.0, 0.0, 66.0)).Value
                           * matrices[1]);

            matrices.Add(new TranslateTransform3D(0.0, 0.0, 66.0).Value
                          * new RotateTransform3D(
                              new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), -15.47 + angles[2]),
                              new Point3D(0.0, 0.0, 66.0)).Value
                           * matrices[1]);

            matrices.Add(new TranslateTransform3D(-43.37, 0.0, 54.0).Value
                          * new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[1] - angles[2]),new Point3D(-43.37, 0.0, 54.0)).Value
                          * new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[2]), new Point3D(0.0, 0.0, 64.0)).Value
                          * matrices[1]);

            matrices.Add(new TranslateTransform3D(-81.722, 0.0, 57.63299999999998).Value
                          * new RotateTransform3D(
                              new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), -angles[1] + angles[2]),
                              new Point3D(-81.722, 0.0, 57.63299999999998)).Value
                           * matrices[2]);

            matrices.Add(new TranslateTransform3D(15.34, 0.0, 31.5).Value
                          * new RotateTransform3D(
                              new AxisAngleRotation3D(new Vector3D(1.0, 0.0, 0.0), angles[3]),
                              new Point3D(15.34, 0.0, 31.5)).Value 
                           * matrices[5]);

            matrices.Add(new TranslateTransform3D(41.5, 3.5, 0.0).Value
                         * new RotateTransform3D(
                              new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), angles[4]),
                              new Point3D(41.5, 3.5, 0.0)).Value
                           * matrices[6]);

            matrices.Add(new TranslateTransform3D(86.925, angles[5] * 0.8, 0.0).Value
                         * matrices[7]);
            matrices.Add(new TranslateTransform3D(86.925, -angles[5] * 0.8, 0.0).Value 
                         * matrices[7]);
            return matrices;
        }

        private void Move(List<Matrix3D> matrix3Ds)
        {
            OnPlay?.Invoke(matrix3Ds);
        }

        private void ResetManipulatorDirection()
        {
            var tt = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0)).Value;
            var combinedTransform = new Transform3DGroup();
            combinedTransform.Children.Add(new MatrixTransform3D(tt));
            ManipulatorTransform = combinedTransform;
        }
        #endregion

        #region MouseDown
        public void OnMouseDown3DHandler(object sender, MouseDown3DEventArgs e)
        {
            if (e.HitTestResult != null 
             && e.HitTestResult.ModelHit is MeshGeometryModel3D m)
            {
                if (IsUIMeshGeometryModel3D(m))
                {
                    Target = null;
                    CenterOffset = m.Geometry.Bound.Center; // Must update this before updating target        
                    SizeScale = GetBoundBoxMaxWidth(m);
                    SelectObject = m;
                    Target = e.HitTestResult.ModelHit as Element3D;
                    //Trace.WriteLine($" Mouse Down {DateTime.Now} Postion {viewport.CursorPosition?.ToString()}");
                }
                else
                {
                    if (SelectObject is Element3D element3D)
                    {
                        PreMatrix = element3D.SceneNode.TotalModelMatrix;
                        PreMatrix.Decompose(out var scale, out var rotation, out var translation);
                        var scaleMatrix = Matrix.Scaling(scale);
                        var rotationMatrix = Matrix.RotationQuaternion(rotation);
                        var translationMatrix = Matrix.Translation(translation);
                        Trace.WriteLine($"DOWN Scale {scale} Rotation {rotation} Translation {translation}");
                    }
                }
            }
        }

        public void OnMouseUp3DDHandler(object sender, MouseUp3DEventArgs e)
        {
            if (e.HitTestResult != null 
                && e.HitTestResult.ModelHit is MeshGeometryModel3D m  )
            {
                //选中的是模型节点
                if (IsUIMeshGeometryModel3D(m))
                {
                    Target = null;
                    CenterOffset = m.Geometry.Bound.Center; // Must update this before updating target
                    Target = e.HitTestResult.ModelHit as Element3D;
                    SizeScale = GetBoundBoxMaxWidth(m);
                    Trace.WriteLine($" Mouse Down {DateTime.Now} Postion {viewport.CursorPosition?.ToString()}");
                }
                // 选中的是Manipulator
                else
                {
                    if(SelectObject is Element3D element3D)
                    {
                        CurrentMatrix = element3D.SceneNode.TotalModelMatrix;
                        CurrentMatrix.Decompose(out var scale, out var rotation, out var translation);

                        var scaleMatrix = Matrix.Scaling(scale);
                        var rotationMatrix = Matrix.RotationQuaternion(rotation);
                        var translationMatrix =  Matrix.Translation(translation);
                        Trace.WriteLine($"UP   Scale {scale} Rotation {rotation} Translation {translation}");

                        CalQuaternion();
                    }
                }
            }
        }

        private double GetBoundBoxMaxWidth(MeshGeometryModel3D meshModel)
        {
            var geometry = meshModel.Geometry as MeshGeometry3D;
            var boundingBox = geometry.Bound;
            var width = boundingBox.Size.X;
            var height = boundingBox.Size.Y;
            var depth = boundingBox.Size.Z;

            var maxDimension = Math.Max(width, Math.Max(height, depth))/2;
            return maxDimension;
        }
        #endregion

        #region 计算通用方法

        private void CalQuaternion()
        {
            PreMatrix.Decompose(out var preScale, out var preRotation, out var preTranslation);
            CurrentMatrix.Decompose(out var scale, out var rotation, out var translation);

            //弧度制旋转角度
            var aa = preRotation.Angle;
            var bb = rotation.Angle;

            float preangle = 2.0f * (float)Math.Acos(preRotation.W);
            float preangleInDegrees = preangle * (180.0f / (float)Math.PI);

            float angle = 2.0f * (float)Math.Acos(rotation.W);

            //角度制
            float angleInDegrees = angle * (180.0f / (float)Math.PI);


            //double angleInRadians = HelixSharpDemo.Model.Quaternion.GetRotationAngle(preRotation, rotation);
            //double angleInDegrees = angleInRadians * (180 / Math.PI);
        }

        private float AngleToDegree(Quaternion rotation)
        {
            // 计算角度（弧度制）
            float angleInRadians = 2.0f * (float)Math.Acos(rotation.W);

            // 计算旋转轴，注意需要归一化
            float sinThetaOverTwo = (float)Math.Sqrt(1.0 - rotation.W * rotation.W);
            Vector3 axis;

            if (sinThetaOverTwo > 0.0001f) // 避免除以零的情况
            {
                axis = new Vector3(rotation.X / sinThetaOverTwo, rotation.Y / sinThetaOverTwo, rotation.Z / sinThetaOverTwo);
            }
            else
            {
                // 如果角度非常小，默认选择一个轴
                axis = new Vector3(1.0f, 0.0f, 0.0f); // 任何轴都可以
            }

            // 将角度转换为角度制
            float angleInDegrees = angleInRadians * (180.0f / (float)Math.PI);

            // 判断旋转轴，决定角度的正负
            if (axis.Z < 0)  // 假设 z 轴为基准
            {
                angleInDegrees = -angleInDegrees;
            }

            Console.WriteLine($"旋转轴: {axis}");
            Console.WriteLine($"旋转角度: {angleInDegrees} 度");
            return angleInDegrees;
        }


        #endregion


        public void InitRobotTransform()
        {
            RotateTransform3D R = new RotateTransform3D();
            TranslateTransform3D T = new TranslateTransform3D();
            F1 = new Transform3DGroup();
            F2 = new Transform3DGroup();
            F3 = new Transform3DGroup();
            F4 = new Transform3DGroup();
            F5 = new Transform3DGroup();
            F6 = new Transform3DGroup();
            F7 = new Transform3DGroup();
            F8 = new Transform3DGroup();
            F9 = new Transform3DGroup();
            F10 = new Transform3DGroup();

            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), 0.0), new Point3D(0.0, 0.0, 0.0));
            F1.Children.Add(R);

            T = new TranslateTransform3D(0.0, 0.0, 77.9);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 0.0, 1.0), 0.0), new Point3D(0.0, 0.0, 77.9));
            F2.Children.Add(T);
            F2.Children.Add(R);
            F2.Children.Add(F1);

            T = new TranslateTransform3D(0.0, 0.0, 66.0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), 0.0), new Point3D(0.0, 0.0, 66.0));
            F3.Children.Add(T);
            F3.Children.Add(R);
            F3.Children.Add(F2);

            T = new TranslateTransform3D(0.0, 0.0, 66.0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), -15.47), new Point3D(0.0, 0.0, 66.0));
            F4.Children.Add(T);
            F4.Children.Add(R);
            F4.Children.Add(F2);

            T = new TranslateTransform3D(-43.37, 0.0, 54.0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), 0.0), new Point3D(0.0, 0.0, 66.0));
            F5.Children.Add(T);
            F5.Children.Add(R);
            F5.Children.Add(F2);

            T = new TranslateTransform3D(-81.722, 0.0, 57.63299999999998);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), 0.0), new Point3D(-81.722, 0.0, 57.63299999999998));
            F6.Children.Add(T);
            F6.Children.Add(R);
            F6.Children.Add(F3);

            T = new TranslateTransform3D(15.54, 0.0, 31.7);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1.0, 0.0, 0.0), 0.0), new Point3D(15.54, 0.0, 31.7));
            F7.Children.Add(T);
            F7.Children.Add(R);
            F7.Children.Add(F6);

            T = new TranslateTransform3D(41.5, 3.5, 0.0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0.0, 1.0, 0.0), 0.0), new Point3D(41.5, 0.0, 0.0));
            F8.Children.Add(T);
            F8.Children.Add(R);
            F8.Children.Add(F7);

            T = new TranslateTransform3D(86.925, 0.0, 0.0);
            F9.Children.Add(T);
            F9.Children.Add(F8);

            T = new TranslateTransform3D(86.925, 0.0, 0.0);
            F10.Children.Add(T);
            F10.Children.Add(F8);
        }

        private void RobotInitTransform(string path, SceneNode sceneNode)
        {
            var finalMatrix = DefaultMatrix3D;
            //初始位姿
            //var finalMatrix = Matrix3D.Identity;

            if (path.Contains("part0"))
            {
                sceneNode.ModelMatrix = F1.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part1"))
            {
                sceneNode.ModelMatrix = F2.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part2"))
            {
                sceneNode.ModelMatrix = F3.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part3"))
            {
                sceneNode.ModelMatrix = F4.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part4"))
            {
                sceneNode.ModelMatrix = F5.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part5"))
            {
                sceneNode.ModelMatrix = F6.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part6"))
            {
                sceneNode.ModelMatrix = F7.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part7"))
            {
                sceneNode.ModelMatrix = F8.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part8"))
            {
                sceneNode.ModelMatrix = F9.ToMatrix() * finalMatrix.ToMatrix();
            }
            else if (path.Contains("part9"))
            {
                sceneNode.ModelMatrix = F10.ToMatrix() * finalMatrix.ToMatrix();
            }else
            {
                sceneNode.ModelMatrix = finalMatrix.ToMatrix(); 
            }
        }
    }
}
