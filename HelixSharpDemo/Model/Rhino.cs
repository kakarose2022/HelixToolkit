

using Rhino.Geometry;
using RobotLib;

namespace HelixSharpDemo.Model
{
    internal class Rhino
    {
        public static Transform TrInterp(Transform T0, Transform T1, double ratio)
        {
            Plane plane0 = T0.ToPlane();
            Plane plane1 = T1.ToPlane();
            // 提取旋转和平移部分
            Quaternion q0 = plane0.ToQuaternion();
            Quaternion q1 = plane1.ToQuaternion();
            Vector3d p0 = new Vector3d(plane0.OriginX, plane0.OriginY, plane0.OriginZ);
            Vector3d p1 = new Vector3d(plane1.OriginX, plane1.OriginY, plane1.OriginZ);

            // 对四元数进行球面线性插值 (Slerp)
            Quaternion qr = Quaternion.Slerp(q0, q1, ratio);

            // 对平移向量进行线性插值
            Vector3d pr = (1 - ratio) * p0 + ratio * p1;

            // 构造新的 Transform，基于插值结果
            Transform result = Transform.Identity;
            result = Transform.Translation(pr) * qr.ToTransform();

            return result;
        }

        public static List<Transform> LinePoseInterp(Transform startPose, Transform endPose, int n)
        {
            List<Transform> allT = new List<Transform>();
            for (int i = 0; i < n; i++)
            {
                double r = (double)i / (n - 1);
                Transform interpolatedT = TrInterp(startPose, endPose, r);
                allT.Add(interpolatedT);
            }

            return allT;
        }
    }
}
