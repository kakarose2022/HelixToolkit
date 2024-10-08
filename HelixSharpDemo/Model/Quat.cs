using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelixSharpDemo.Model
{
    public struct Quaternion
    {
        public double W, X, Y, Z;

        public Quaternion(double w, double x, double y, double z)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        // 计算共轭
        public Quaternion Conjugate()
        {
            return new Quaternion(W, -X, -Y, -Z);
        }

        // 计算旋转四元数
        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(
                q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z,
                q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
                q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X,
                q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W
            );
        }

        // 计算旋转角度
        public static double GetRotationAngle(Quaternion qBefore, Quaternion qAfter)
        {
            Quaternion qRotation = qAfter * qBefore.Conjugate();
            return 2 * Math.Acos(qRotation.W); // 旋转角度（弧度）
        }
    }

}
