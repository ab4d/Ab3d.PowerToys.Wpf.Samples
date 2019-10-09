using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    public partial class CameraControllerIntroPage : Page
    {
        public CameraControllerIntroPage()
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