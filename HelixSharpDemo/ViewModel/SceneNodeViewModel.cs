using HelixToolkit.SharpDX.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HelixSharpDemo.ViewModel
{
    public class SceneNodeViewModel : ObservableObject
    {
        //旋转轴
        public AxisTye axisTye { get; set; }
        //以原点旋转中心
        public bool isBoundCenter { get; set; }
        public string pathName { get; set; }

        public SceneNodeViewModel(string path)
        {
            if (path.Contains("part0"))
            {
                axisTye = AxisTye.Y;
                isBoundCenter = true;      
            }
            else if (path.Contains("part1"))
            {
                axisTye = AxisTye.Y;
                isBoundCenter = false;
                pathName = path;
            }
            else if (path.Contains("part2"))
            {
                axisTye = AxisTye.Z;
                isBoundCenter = false;
                pathName = path;
            }
            else
            {
                axisTye = AxisTye.Y;
                isBoundCenter = false;
                pathName = path;
            }      
        }
    }

    public enum AxisTye 
    { 
        X,
        Y,
        Z
    }
}
