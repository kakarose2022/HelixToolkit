using System.Configuration;
using System.Data;
using System.Windows;

namespace HelixSharpDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
            //this.StartupUri = new Uri("/View/DemoView.xaml", UriKind.Relative);
        }
    }

}
