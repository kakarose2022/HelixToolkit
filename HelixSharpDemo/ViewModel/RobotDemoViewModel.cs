using HelixToolkit.SharpDX.Core.Animations;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using System.IO;

namespace HelixSharpDemo.ViewModel
{
    public class RobotDemoViewModel : BaseViewModel
    {
        public SceneNodeGroupModel3D ModelGroup { get; } = new SceneNodeGroupModel3D();

        private bool showWireframe = false;
        public bool ShowWireframe
        {
            set
            {
                if (Set(ref showWireframe, value))
                {
                    foreach (var m in boneSkinNodes)
                    {
                        m.RenderWireframe = value;
                    }
                }
            }
            get
            {
                return showWireframe;
            }
        }

        private bool showSkeleton = false;
        public bool ShowSkeleton
        {
            set
            {
                if (Set(ref showSkeleton, value))
                {
                    foreach (var m in skeletonNodes)
                    {
                        m.Visible = value;
                    }
                }
            }
        }

        private bool enableAnimation = true;
        public bool EnableAnimation
        {
            set
            {
                enableAnimation = value;
                if (enableAnimation)
                {
                    compositeHelper.Rendering += CompositeHelper_Rendering;
                }
                else
                {
                    compositeHelper.Rendering -= CompositeHelper_Rendering;
                }
            }
            get
            {
                return enableAnimation;
            }
        }

        private string selectedAnimation;
        public string SelectedAnimation
        {
            set
            {
                if (Set(ref selectedAnimation, value))
                {
                    reset = true;
                    var curr = scene.Animations.Where(x => x.Name == value).FirstOrDefault();
                    animationUpdater = new NodeAnimationUpdater(curr);
                    animationUpdater.RepeatMode = selectedRepeatMode;
                }
            }
            get { return selectedAnimation; }
        }

        private AnimationRepeatMode selectedRepeatMode = AnimationRepeatMode.Loop;
        public AnimationRepeatMode SelectedRepeatMode
        {
            set
            {
                if (Set(ref selectedRepeatMode, value))
                {
                    reset = true;
                    if (animationUpdater != null) animationUpdater.RepeatMode = value;
                }
            }
            get { return selectedRepeatMode; }
        }

        public System.Windows.Media.Media3D.Transform3D ModelTransform { private set; get; }

        public LineGeometry3D HitLineGeometry { get; } = new LineGeometry3D() { IsDynamic = true };

        public string[] Animations { set; get; }

        public GridPattern[] GridTypes { get; } = new GridPattern[] { GridPattern.Tile, GridPattern.Grid };

        public AnimationRepeatMode[] RepeatModes { get; } = new AnimationRepeatMode[] { AnimationRepeatMode.Loop, AnimationRepeatMode.PlayOnce, AnimationRepeatMode.PlayOnceHold };

        private const int NumSegments = 100;
        private const int Theta = 24;
        private long startAniTime = 0;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private SynchronizationContext context = SynchronizationContext.Current;

        private bool reset = true;
        private HelixToolkitScene scene;
        private NodeAnimationUpdater animationUpdater;
        private List<BoneSkinMeshNode> boneSkinNodes = new List<BoneSkinMeshNode>();
        private List<BoneSkinMeshNode> skeletonNodes = new List<BoneSkinMeshNode>();
        private CompositionTargetEx compositeHelper = new CompositionTargetEx();

        public RobotDemoViewModel()
        {
            this.Title = "BoneSkin Demo";
            this.SubTitle = "WPF & SharpDX";
            EffectsManager = new DefaultEffectsManager();

            this.Camera = new PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(50, 50, 50),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(-50, -50, -50),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                NearPlaneDistance = 1,
                FarPlaneDistance = 2000
            };
            HitLineGeometry.Positions = new Vector3Collection(2);
            HitLineGeometry.Positions.Add(Vector3.Zero);
            HitLineGeometry.Positions.Add(Vector3.Zero);
            HitLineGeometry.Indices = new IntCollection(2);
            HitLineGeometry.Indices.Add(0);
            HitLineGeometry.Indices.Add(1);
            LoadFile();
            compositeHelper.Rendering += CompositeHelper_Rendering;
        }

        private void LoadFile()
        {
            var importer = new Importer();
            importer.Configuration.CreateSkeletonForBoneSkinningMesh = true;
            importer.Configuration.SkeletonSizeScale = 0.04f;
            importer.Configuration.GlobalScale = 0.1f;

            var path = Path.Combine(Environment.CurrentDirectory, "stl");
            path = Path.Combine(path, "Solus_The_Knight.fbx");
            scene = importer.Load(path);


            ModelGroup.AddNode(scene.Root);
            Animations = scene.Animations.Select(x => x.Name).ToArray();
            foreach (var node in scene.Root.Items.Traverse(false))
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

        private void HandleMouseDown(object sender, SceneNodeMouseDownArgs e)
        {
            var result = e.HitResult;
            HitLineGeometry.Positions[0] = result.PointHit - result.NormalAtHit * 0.5f;
            HitLineGeometry.Positions[1] = result.PointHit + result.NormalAtHit * 0.5f;
            HitLineGeometry.UpdateVertices();
        }

        private void CompositeHelper_Rendering(object sender, System.Windows.Media.RenderingEventArgs e)
        {
            if (animationUpdater != null)
            {
                if (reset)
                {
                    animationUpdater.Reset();
                    animationUpdater.RepeatMode = SelectedRepeatMode;
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

        protected override void Dispose(bool disposing)
        {
            cts.Cancel(true);
            compositeHelper.Dispose();
            base.Dispose(disposing);
        }
    }
}
