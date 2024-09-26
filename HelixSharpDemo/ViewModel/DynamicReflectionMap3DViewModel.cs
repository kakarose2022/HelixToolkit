using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Assimp;
using System.Windows.Media.Media3D;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using System.IO;
using System.Windows.Input;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.OrthographicCamera;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model;
using System.Windows.Media;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using SharpDX;
using Color = SharpDX.Color;
using GeometryModel3D = HelixToolkit.Wpf.SharpDX.GeometryModel3D;
using System.Diagnostics;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;

namespace HelixSharpDemo.ViewModel
{
    public class DynamicReflectionMap3DViewModel : BaseViewModel
    {
        private HelixToolkitScene scene;

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
                    }
                }
            }
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

        /// <summary>
        ///  所有导入物件的Scene的
        /// </summary>
        private List<MeshGeometryModel3D> meshGeometryModel3Ds;

        public List<MeshGeometryModel3D> MeshGeometryModel3Ds
        {
            get { return meshGeometryModel3Ds; }
            set
            {
                Set(ref meshGeometryModel3Ds, value);
            }
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

        #endregion

        public ICommand SetCameraPostionCommand { private set; get; }
        public ICommand ReloadCameraCommand { private set; get; }

        public delegate void ChangeDyContentEvent();
        public event ChangeDyContentEvent? ChangeDyContent;

        #region transform
        public Point3D CurrentPostion {  set; get; }

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
        public DynamicReflectionMap3DViewModel()
        {
            SceneNodes = new List<HelixToolkitScene>();
            MeshGeometryModel3Ds = new List<MeshGeometryModel3D>();
            CurrentPostion = new Point3D();
            InitSetting();
            ReloadFile();
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

            base.InitSetting();
            Reload();
        }

        protected async void ReloadFile()
        {
            SceneNodes.Clear();
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
                await LoadModelFile(Path.Combine(path, $"p0.stl"));
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

            //this.Camera = new PerspectiveCamera
            //{
            //    Position = new System.Windows.Media.Media3D.Point3D(446, 3000, 2500),
            //    LookDirection = new System.Windows.Media.Media3D.Vector3D(-138, -2777, -2777),
            //    UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            //    NearPlaneDistance = 0.1,
            //    FieldOfView = 45,
            //    FarPlaneDistance = 20000
            //};


            this.Camera = new OrthographicCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(-1, -1, -1),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                NearPlaneDistance = -0.1f,
                FarPlaneDistance = 10000
            };
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

                                    //var normalMap = TextureModel.Create(new System.Uri("TextureNoise1_dot3.dds", System.UriKind.RelativeOrAbsolute).ToString());
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
                                }
                            }
                        }
                        RobotInitTransform(path, loadedScene?.Root);
                        SceneNodeToMeshGeometry3D(loadedScene?.Root);

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
        public IEnumerable<MeshGeometryModel3D> SceneNodeToMeshGeometry3D(SceneNode node)
        {
            foreach (var single in node.Traverse())
            {
                if(single is MeshNode meshNode)
                {
                   var model =  new MeshGeometryModel3D
                    {
                        Geometry = meshNode.Geometry,
                        CullMode = SharpDX.Direct3D11.CullMode.Back,
                        Material = ConvertMaterial(meshNode.Material),
                        IsThrowingShadow = true,
                      
                    };
                    Transform(model);
                    yield return model;
                }
            }
        }

        private void Transform(MeshGeometryModel3D model)
        {
            CurrentPostion = model.Transform.Transform(new Point3D(0, 0, 0));
            var rotationX = new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(1, 0, 0), 270));
            var rotationY = new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(0, 1, 0), 0));
            var combinedTransform = new Transform3DGroup();
            combinedTransform.Children.Add(rotationX);
            combinedTransform.Children.Add(rotationY);
            model.Transform = combinedTransform;
        }

        private bool IsUIMeshGeometryModel3D(MeshGeometryModel3D model)
        {
            return MeshGeometryModel3Ds.Any(o => o == model);
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


        #region MouseDown
        public void OnMouseDown3DHandler(object sender, MouseDown3DEventArgs e)
        {
            if (e.HitTestResult != null 
                && e.HitTestResult.ModelHit is MeshGeometryModel3D m
                && IsUIMeshGeometryModel3D(m))
            {
                Trace.WriteLine($" Mouse Down{DateTime.Now}");

                Target = null;
                CenterOffset = m.Geometry.Bound.Center; // Must update this before updating target
                Target = e.HitTestResult.ModelHit as Element3D;
                SizeScale = GetBoundBoxMaxWidth(m);
                Trace.WriteLine($" Mouse Down End {CenterOffset}");
            }
        }

        public void OnMouseUp3DDHandler(object sender, MouseUp3DEventArgs e)
        {
            if (e.HitTestResult != null 
                && e.HitTestResult.ModelHit is MeshGeometryModel3D m 
                && IsUIMeshGeometryModel3D(m))
            {
                Trace.WriteLine($" Mouse Up{DateTime.Now}");
                Target = null;
                CenterOffset = m.Geometry.Bound.Center; // Must update this before updating target
                Target = e.HitTestResult.ModelHit as Element3D;
                Trace.WriteLine($" Mouse UP End {CenterOffset}");
            }
        }

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
        #endregion

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
    }
}
