using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Ab3d.PowerToys.Samples.Input
{
    public partial class InputIntroPage : Page
    {
        public InputIntroPage()
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