using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Ab3d.PowerToys.Samples.UseCases
{
    public partial class UseCasesIntroPage : Page
    {
        public UseCasesIntroPage()
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