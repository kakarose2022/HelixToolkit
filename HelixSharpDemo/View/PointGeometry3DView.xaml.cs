using HelixSharpDemo.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HelixSharpDemo.View
{
    /// <summary>
    /// PointGeometry3DView.xaml 的交互逻辑
    /// </summary>
    public partial class PointGeometry3DView : UserControl
    {
        public PointGeometry3DView()
        {
            this.DataContext = new PointGeometry3DViewModel();
            InitializeComponent();
        }
    }
}
