using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Ab3d.PowerToys.Samples.Cameras
{
    public partial class CamerasIntroPage : Page
    {
        public CamerasIntroPage()
        {
            InitializeComponent();
        }

        private void link_navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
            e.Handled = true;
        }
    }
}