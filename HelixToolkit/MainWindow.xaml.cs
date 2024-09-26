using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HelixToolkit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void HelixViewport3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(sender is HelixToolkit.Wpf.HelixViewport3D viewport)
            {
                var pt = e.GetPosition(viewport);
            }
         

        }
    }
}