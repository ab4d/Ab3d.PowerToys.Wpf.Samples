using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using Ab3d.Cameras;
using Ab3d.PowerToys.Samples.Common;

namespace Ab3d.PowerToys.Samples.Other
{
    public partial class IntroductionPage : Page
    {
        public IntroductionPage()
        {
            InitializeComponent();

            // Revision number defines the type of assembly:
            // version from    0 ... 999  - evaluation version from evaluation installer
            // version from 1000 ... 1999 - commercial version from commercial installer
            // version above 2000         - version from NuGet

            bool isNuGetVersion = typeof(BaseCamera).Assembly.GetName().Version.Revision >= 2000;

            // Show info that for .Net Core and .Net 5.0+ it is recommended to use versions from NuGet
            NuGetVersionInfoTextBlockEx.Visibility = isNuGetVersion ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}