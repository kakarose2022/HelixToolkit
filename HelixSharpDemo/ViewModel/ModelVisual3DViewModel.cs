
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Animations;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Microsoft.Win32;
using SharpDX;
using SharpDX.Direct3D9;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using static HelixSharpDemo.ViewModel.ModelVisual3DViewModel;
using Colors = System.Windows.Media.Colors;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.OrthographicCamera;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace HelixSharpDemo.ViewModel
{
    public class ModelVisual3DViewModel : BaseViewModel
    {
        private string OpenFileFilter = $"{HelixToolkit.SharpDX.Core.Assimp.Importer.SupportedFormatsString}";
        public ICommand AddCoordiateCommand { private set; get; }
        public ICommand AddSigleCoordiateCommand { private set; get; }
        public ICommand LoadStlCommand { private set; get; }
        public ICommand SetCameraPostionCommand { private set; get; }
        public TextureModel EnvironmentMap { private set; get; }
        private HelixToolkitScene scene;
        public SceneNodeGroupModel3D GroupModel { get; } = new SceneNodeGroupModel3D();

        private long startAniTime = 0;
        public ObservableCollection<Animation> Animations { get; } = new ObservableCollection<Animation>();
        private NodeAnimationUpdater animationUpdater;
        private bool reset = true;

        #region propfull
        private Geometry3D dyGeometry3d;

        public Geometry3D DyGeometry3d
        {
            get { return dyGeometry3d; }
            set 
            {
                Set(ref dyGeometry3d, value); 
            }
        }

        private List<Geometry3D> geometry3D;

        public List<Geometry3D> Geometry3Ds
        {
            get { return geometry3D; }
            set 
            { 
                Set(ref geometry3D, value); 
            }
        }

        private PBRMaterial pBRMaterial;

        public PBRMaterial PBRMaterial
        {
            get { return pBRMaterial; }
            set {
                Set(ref pBRMaterial, value);
            }
        }

        private Point3D modelCentroid = default;
        public Point3D ModelCentroid
        {
            private set => Set(ref modelCentroid, value);
            get => modelCentroid;
        }

        private BoundingBox modelBound = new BoundingBox();
        public BoundingBox ModelBound
        {
            private set => Set(ref modelBound, value);
            get => modelBound;
        }

        private bool renderEnvironmentMap = true;
        public bool RenderEnvironmentMap
        {
            set
            {
                if (Set(ref renderEnvironmentMap, value) && scene != null && scene.Root != null)
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

        private string textInfo;
        public string TextInfo
        {
            get { return textInfo; }
            set 
            {
                Set(ref textInfo, value); 
            }
        }

        private string finishInfo;
        public string FinishInfo
        {
            get { return finishInfo; }
            set
            {
                Set(ref finishInfo, value);
            }
        }

        public System.Windows.Media.Color Light1Color { get; set; }

        private bool isLoading = false;
        public bool IsLoading
        {
            private set => Set(ref isLoading, value);
            get => isLoading;
        }


        private string selectedAnimation;
        public string SelectedAnimation
        {
            set
            {
                if (Set(ref selectedAnimation, value))
                {
                    //reset = true;
                    //var curr = scene.Animations.Where(x => x.Name == value).FirstOrDefault();
                    //animationUpdater = new NodeAnimationUpdater(curr);
                    //animationUpdater.RepeatMode = selectedRepeatMode;
                }
            }
            get { return selectedAnimation; }
        }
        #endregion

        #region transform
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
        private CompositionTargetEx compositeHelper = new CompositionTargetEx();

        public ModelVisual3DViewModel()
        {
            Init();
            InitRobotTransform();
            compositeHelper.Rendering += CompositeHelper_Rendering;

            EffectsManager = new DefaultEffectsManager();
            LoadStlCommand = new RelayCommand(async o =>
            {
               await OpenFile();
            });

            SetCameraPostionCommand = new RelayCommand(o =>
            {
                SetDefaultCameraView();
            });
        }

        public void Init()
        {
            Light1Direction = new Vector3D(-10, -10, -10);
            Light1Color = Colors.White;
            TextInfo = "初始化中...";
            Geometry3Ds = new List<Geometry3D>();

            InitDefaultCoordiate();

            Camera = new OrthographicCamera()
            {
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, -10),
                Position = new System.Windows.Media.Media3D.Point3D(0, 10, 10),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FarPlaneDistance = 10000,
                NearPlaneDistance = -10
            };
            EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");
        }

        public void InitDefaultCoordiate()
        {
            var builder = new MeshBuilder(true, false, false);

            builder.AddArrow(Vector3.Zero, new Vector3(5.5f, 0, 0), 0.6f, 1.7f, 8);
            builder.AddArrow(Vector3.Zero, new Vector3(0, 5.5f, 0), 0.6f, 1.7f, 8);
            builder.AddArrow(Vector3.Zero, new Vector3(0, 0, 5.5f), 0.6f, 1.7f, 8);

            var mesh = builder.ToMesh();
            //arrowMeshModel.Geometry = mesh;
            //UpdateAxisColor(arrowMeshModel.Geometry, 0, AxisXColor, LabelX, LabelColor);
            //UpdateAxisColor(arrowMeshModel.Geometry, 1, AxisYColor, LabelY, LabelColor);
            //UpdateAxisColor(arrowMeshModel.Geometry, 2, AxisZColor, LabelZ, LabelColor);

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

        private async Task OpenFile()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (isLoading)
            {
                return;
            }

            var paths = OpenFileDialog(OpenFileFilter);
            if (paths == null || !paths.Any())
            {
                return;
            }

            StopAnimation();
            GroupModel.Clear();

            try
            {
                await Task.WhenAll(paths.Select(path => LoadModelFileAndModelBound(path)));
                FocusCameraToScene();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}");
            }
            stopwatch.Stop();
            TextInfo = "加载完成";
            FinishInfo = $"耗时{stopwatch.Elapsed.TotalSeconds.ToString("F2")}s";
            await Task.Delay(4000);
            ClearInfo();
        }

        public async Task LoadModelFileAndModelBound(string path)
        {
            var syncContext = SynchronizationContext.Current;
            var scene = await LoadModelFile(path);
            if (scene != null)
            {
                scene.Root.Attach(EffectsManager); // Pre attach scene graph
                scene.Root.UpdateAllTransformMatrix();
                if (scene.Root.TryGetBound(out var bound))
                {
                    /// Must use UI thread to set value back.
                    syncContext.Post((o) => { ModelBound = bound; }, null);
                }
                if (scene.Root.TryGetCentroid(out var centroid))
                {
                    /// Must use UI thread to set value back.
                    syncContext.Post((o) => { ModelCentroid = centroid.ToPoint3D(); }, null);
                }
            }
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
                    //loader.Configuration.CreateSkeletonForBoneSkinningMesh = true;
                    //loader.Configuration.SkeletonSizeScale = 0.04f;
                    //loader.Configuration.GlobalScale = 0.1f;
                    var loadedScene = await Task.Run(() => loader.Load(path));
      
                    if (loadedScene != null)
                    {
                        if (loadedScene.Root != null)
                        {
                            foreach (var node in loadedScene.Root.Traverse())
                            {
                                if (node is MaterialGeometryNode m)
                                {
                                    //if (m.Material is PBRMaterialCore pbr)
                                    //{
                                    //    pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                    //}
                                    //else if (m.Material is PhongMaterialCore phong)
                                    //{
                                    //    phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                    //}

                                    //m.Material = new PBRMaterial()
                                    //{
                                    //    AlbedoColor = System.Windows.Media.Colors.Blue.ToColor4(),
                                    //    RoughnessFactor = 0,
                                    //    MetallicFactor = 0.5,
                                    //    RenderEnvironmentMap = true,
                                    //    EnableAutoTangent = true,
                                    //    RenderShadowMap = true,
                                    //    ReflectanceFactor = 1,
                                    //    ClearCoatRoughness = 1,
                                    //    ClearCoatStrength = 1,
                                    //};

                                   // m.IsThrowingShadow = true;

                                }

                                 if (node is BoneSkinMeshNode bone)
                                {
                                    if (!bone.IsSkeletonNode)
                                    {
                                        bone.IsThrowingShadow = true;
                                        bone.WireframeColor = new SharpDX.Color4(0, 0, 1, 1);
                                        //boneSkinNodes.Add(m);
                                        //m.MouseDown += HandleMouseDown;
                                    }
                                    else
                                    {
                                        //skeletonNodes.Add(m);
                                        bone.Visible = false;
                                    }
                                }
                            }

                            foreach (var node in loadedScene.Root.Items.Traverse(false))
                            {
                                if (node is BoneSkinMeshNode m)
                                {
                                    if (!m.IsSkeletonNode)
                                    {
                                        m.IsThrowingShadow = true;
                                        m.WireframeColor = new SharpDX.Color4(0, 0, 1, 1);
                                        //boneSkinNodes.Add(m);
                                        //m.MouseDown += HandleMouseDown;
                                    }
                                    else
                                    {
                                        //skeletonNodes.Add(m);
                                        m.Visible = false;
                                    }
                                }
                            }
                        }
                        //RobotInitTransform(path, loadedScene?.Root);
                        GroupModel.AddNode(loadedScene?.Root);
                        //SceneNodeToMeshGeometry3D(loadedScene?.Root);

                        if (loadedScene.HasAnimation)
                        {
                            foreach (var ani in loadedScene.Animations)
                            {
                                Animations.Add(ani);
                            }
                        }
                    }
                    TextInfo = "加载完成";
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

        private void RobotInitTransform(string path, SceneNode sceneNode)
        {
            if (path.Contains("part0"))
            {
                sceneNode.ModelMatrix = F1.ToMatrix();
            }
            else if (path.Contains("part1"))
            {
                sceneNode.ModelMatrix = F2.ToMatrix();
            }
            else if (path.Contains("part2"))
            {
                sceneNode.ModelMatrix = F3.ToMatrix();
            }
            else if (path.Contains("part3"))
            {
                sceneNode.ModelMatrix = F4.ToMatrix();
            }
            else if (path.Contains("part4"))
            {
                sceneNode.ModelMatrix = F5.ToMatrix();
            }
            else if (path.Contains("part5"))
            {
                sceneNode.ModelMatrix = F6.ToMatrix();
            }
            else if (path.Contains("part6"))
            {
                sceneNode.ModelMatrix = F7.ToMatrix();
            }
            else if (path.Contains("part7"))
            {
                sceneNode.ModelMatrix = F8.ToMatrix();
            }
            else if (path.Contains("part8"))
            {
                sceneNode.ModelMatrix = F9.ToMatrix();
            }
            else if (path.Contains("part9"))
            {
                sceneNode.ModelMatrix = F10.ToMatrix();
            }
        }

        private void FocusCameraToScene()
        {

            var maxWidth = Math.Max(Math.Max(modelBound.Width, modelBound.Height), modelBound.Depth);
            var pos = modelBound.Center + new Vector3(0, 0, maxWidth);
            Camera.Position = pos.ToPoint3D();

            //Camera.LookDirection = (modelBound.Center - pos).ToVector3D();
            //var lookDirection = (modelBound.Center - pos).ToVector3D();
            var lookDirection = (modelBound.Center - pos);
            float angleInDegrees = 30.0f;
            float angleInRadians = MathUtil.DegreesToRadians(angleInDegrees);
            Matrix rotationMatrix = Matrix.RotationY(angleInRadians);
            Vector3 newLookDirection = Vector3.TransformNormal(lookDirection, rotationMatrix);
            Camera.LookDirection = newLookDirection.ToVector3D();

            Camera.UpDirection = Vector3.UnitY.ToVector3D();
            if (Camera is OrthographicCamera orthCam)
            {
                orthCam.Width = maxWidth;
            }
        }

        private void SceneNodeToMeshGeometry3D(SceneNode node)
        {
            var geometrys = node.Traverse().Where(x => (x is MeshNode)).Select(m => ((MeshNode)m).Geometry).ToArray();
            foreach (var geometry in geometrys)
            {
                Geometry3Ds.Add(geometry);
            }

            DyGeometry3d = geometrys[0];
            PBRMaterial = new PBRMaterial()
            {
                AlbedoColor = System.Windows.Media.Colors.Blue.ToColor4(),
                RoughnessFactor = 0.5,
                MetallicFactor = 0.7,
                RenderEnvironmentMap = true,
                EnableAutoTangent = true,
                RenderShadowMap = true,
                ReflectanceFactor = 1,
                ClearCoatRoughness = 1,
                ClearCoatStrength = 1,
            };
        }

        private void SetDefaultCameraView()
        {
            Camera = new OrthographicCamera()
            {
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, -10),
                Position = new System.Windows.Media.Media3D.Point3D(0, 10, 10),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FarPlaneDistance = 10000,
                NearPlaneDistance = -10
            };

            FocusCameraToScene();
        }

        private void StopAnimation()
        {
            Animations.Clear();

        }

        private void CompositeHelper_Rendering(object sender, System.Windows.Media.RenderingEventArgs e)
        {
            if (animationUpdater != null)
            {
                if (reset)
                {
                    animationUpdater.Reset();
                    //animationUpdater.RepeatMode = SelectedRepeatMode;
                    reset = false;
                    startAniTime = 0;
                }
                else
                {
                    if (startAniTime == 0)
                    {
                        startAniTime = Stopwatch.GetTimestamp();
                    }
                    var elapsed = Stopwatch.GetTimestamp() - startAniTime;
                    animationUpdater.Update(elapsed, Stopwatch.Frequency);
                }
            }
        }

        private IEnumerable<string> OpenFileDialog(string filter)
        {
            var d = new OpenFileDialog() { Multiselect = true };
            d.CustomPlaces.Clear();
            d.Filter = filter;

            if (!d.ShowDialog().Value)
            {
                return null;
            }
            return d.FileNames;
        }

        private void ClearInfo()
        {
            TextInfo = string.Empty;
            FinishInfo = string.Empty;
        }
    }
}
