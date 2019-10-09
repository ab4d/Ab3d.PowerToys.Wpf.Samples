using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    public partial class AdvancedObjects3DIntroPage : Page
    {
        public AdvancedObjects3DIntroPage()
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