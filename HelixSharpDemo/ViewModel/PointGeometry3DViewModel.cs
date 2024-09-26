using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelixSharpDemo.ViewModel
{
    public class PointGeometry3DViewModel:BaseViewModel
    {
		private PointGeometry3D pointGeometry3D;

		public PointGeometry3D PointGeometry3D
        {
			get { return pointGeometry3D; }
			set 
			{
				Set(ref pointGeometry3D,value);
			}
		}

		public PointGeometry3DViewModel()
		{
			InitSetting();
        }

		protected override void InitSetting()
		{
            base.InitSetting();
			PointGeometry3D = new PointGeometry3D();

            var b2 = new MeshBuilder(true, true, true);
            b2.AddSphere(new Vector3(0f, 0f, 0f), 4, 64, 64);
            var Model = b2.ToMeshGeometry3D();
            PointGeometry3D = new PointGeometry3D()
            {
                IsDynamic = true,
                Positions = Model.Positions
            };
        }

    }
}
