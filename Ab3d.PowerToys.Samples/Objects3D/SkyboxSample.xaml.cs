using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for SkyboxSample.xaml
    /// </summary>
    public partial class SkyboxSample : Page
    {
        public SkyboxSample()
        {
            InitializeComponent();
        }

        private void Camera1_OnCameraChanged(object sender, RoutedEventArgs e)
        {
            SkyBoxCamera.Heading = Camera1.Heading;
            SkyBoxCamera.Attitude = Camera1.Attitude;
        }
    }
}
